// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Common.Strategy;

public static class PricingHelper
{
    private const decimal GrossVanCapacityM3 = 24m;
    private const decimal LoadFactor = 0.85m;
    public const decimal EffectiveVanCapacityM3 = GrossVanCapacityM3 * LoadFactor; // 20.4m³
    public const decimal VatRate = 0.20m;
    public const decimal FloorSurcharge = 25m;

    public static decimal CalculateTotalVolume(QuoteV2 quote)
    {
        return quote.InventoryItems.Sum(item =>
        {
            var unitVolume = CalculateUnitVolumeInCubicMeters(item.Height!.Value, item.Width!.Value, item.Depth!.Value);
            if (unitVolume <= 0)
                throw new InvalidOperationException($"Invalid volume for item {item.Name}");

            if (item.Quantity <= 0)
                throw new InvalidOperationException($"Invalid quantity for item {item.Name}");

            return unitVolume * item.Quantity!.Value;
        });
    }

    public static CapacityResult EvaluateVanCapacity(
        decimal totalVolume,
        int recommendedVans,
        int selectedVans,
        decimal effectiveVanCapacityM3,
        IReadOnlyList<QuoteInventoryItem> items)
    {
        var selectedCapacity = selectedVans * effectiveVanCapacityM3;
        var overflowRatio = totalVolume / selectedCapacity;
        var vanShortfall = recommendedVans - selectedVans;
        var hasSpecialItems = items.Any(IsSpecialHandlingItem);

        // Never return Blocked: customers may continue; surface risk as warnings instead.
        var severeCapacityRisk =
            overflowRatio > 1.35m ||
            vanShortfall >= 2 ||
            (totalVolume > 40m && vanShortfall >= 1) ||
            (hasSpecialItems && overflowRatio > 1.15m);

        if (severeCapacityRisk)
        {
            return new CapacityResult(
                VanCapacityStatus.Warning,
                $"Important: your inventory volume is high relative to {selectedVans} van(s). " +
                $"We strongly recommend {recommendedVans} van(s) for a reliable move. " +
                $"If you continue with fewer vans, some items may be left behind, the crew may not be able to take the full load, " +
                $"or the move may only be partly fulfilled. You can still proceed—increase the van count or contact us if you are unsure.");
        }

        var moderateCapacityRisk =
            vanShortfall == 1 ||
            overflowRatio > 1.10m;

        if (moderateCapacityRisk)
        {
            return new CapacityResult(
                VanCapacityStatus.Warning,
                $"Based on your selected inventory, we strongly recommend {recommendedVans} van(s). " +
                $"You selected {selectedVans}. If fewer vans are chosen, some items may be left behind, " +
                $"or the move may be partially fulfilled.");
        }

        return new CapacityResult(VanCapacityStatus.Recommended, null);
    }

    public static bool IsSpecialHandlingItem(QuoteInventoryItem item)
    {
        var name = item.Name.ToLowerInvariant();

        return name.Contains("tv") ||
               name.Contains("television") ||
               name.Contains("piano") ||
               name.Contains("glass") ||
               name.Contains("mirror") ||
               name.Contains("fragile") ||
               name.Contains("american fridge");
    }

    public static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public sealed record CapacityResult(
        VanCapacityStatus Status,
        string? Warning
    );

    public static int GetFloor(QuoteV2 quote, QuoteAddressKind kind)
    {
        var address = quote.Addresses.First(x => x.Kind == kind);

        return address is { HasElevator: false, Floor: > 0 } ? address.Floor.Value : 0;
    }

    public static async Task<decimal> GetDismantleRate(AdditionalPriceType additionalPriceType, IAdditionalPriceRepository additionalPriceRepository, CancellationToken cancellationToken)
    {
        var additionalPrice = await additionalPriceRepository.GetAdditionalPricesAsync(true, cancellationToken);
        return additionalPrice.First(x => x.Type == additionalPriceType).Price;
    }

    public static decimal CalculateFloorSurcharge(int floor, decimal floorSurcharge, bool hasNoElevatorAndNotGroundFloor)
    {
        if (hasNoElevatorAndNotGroundFloor)
            return 0m;

        return floorSurcharge * floor;
    }

    public static void ProcessSelectedOptions(IClock clock, QuoteV2 quote, Guid pricingId)
    {
        var selected = quote.Pricings.SingleOrDefault(x => x.Id == pricingId);

        if (selected is null)
            throw new InvalidOperationException("Invalid pricing option selected.");

        foreach (var pricing in quote.Pricings)
        {
            pricing.IsSelected = false;
        }

        selected.IsSelected = true;

        var subtotalWithoutVat = selected.SubtotalWithoutVat;

        var vatAmount = subtotalWithoutVat * selected.VatRate;
        var totalWithVat = subtotalWithoutVat + vatAmount;

        selected.SubtotalWithoutVat = subtotalWithoutVat;
        selected.VatAmount = vatAmount;
        selected.TotalCostWithVat = totalWithVat;

        quote.SelectedVanCount = selected.SelectedVanCount;
        quote.CrewCount = selected.CrewCount;
        quote.ServiceTier = selected.ServiceLevel;
        quote.QuotePrice = selected.TotalCostWithVat;
        quote.TotalCost = selected.TotalCostWithVat;
        quote.PriceCalculatedAt = clock.GetCurrentInstant();
    }

    public static async ValueTask<(List<ServiceTextDto> standardTexts, List<ServiceTextDto> premiumTexts, ExtraPricesDto additionalServices)> GetAdditionalServicesAndServiceTextsAsync(IClock clock, IRemovalPricingRepository removalPricingRepository,
        IAdditionalPriceRepository additionalPriceRepository, CancellationToken cancellationToken)
    {
        var features = await removalPricingRepository.GetServiceFeatureAsync(clock.GetCurrentInstant(), cancellationToken);
        var additionalPrices = await additionalPriceRepository.GetAdditionalPricesAsync(true, cancellationToken);

        var standardTexts = features.Where(f => f.ServiceLevel == ServiceLevel.Standard)
            .Select((f, i) => new ServiceTextDto { Id = i + 1, Text = f.Text })
            .ToList();

        var premiumTexts = features.Where(f => f.ServiceLevel == ServiceLevel.Premium)
            .Select((f, i) => new ServiceTextDto { Id = i + 1, Text = f.Text })
            .ToList();

        var additionalServices = new ExtraPricesDto
        {
            Dismantle = additionalPrices
                .Where(p => p.Type == AdditionalPriceType.Dismantle)
                .Select(p => new AdditionalPriceDto
                {
                    Id = p.Id,
                    Description = p.Description,
                    Price = p.Price,
                    CurrencyCode = p.CurrencyCode
                }).FirstOrDefault(),
            Assembly = additionalPrices
                .Where(p => p.Type == AdditionalPriceType.Assembly)
                .Select(p => new AdditionalPriceDto
                {
                    Id = p.Id,
                    Description = p.Description,
                    Price = p.Price,
                    CurrencyCode = p.CurrencyCode
                }).FirstOrDefault()
        };
        return (standardTexts, premiumTexts, additionalServices);
    }

    public static void RecalculateStepState(
        QuoteV2 quote,
        QuoteSteps patchedStep,
        string patchedStepKey,
        IQuoteStepInvalidationService quoteStepInvalidationService,
        IQuoteProgressCalculator progressCalculator,
        IQuoteJourneyProvider quoteJourneyProvider)
    {
        var journey = quoteJourneyProvider.Get(quote.Type);
        var step = journey.Steps.Single(x => x.Flag == patchedStep);

        // Capture state before this PATCH changes completion/dirty flags.
        var previousCompletedSteps = quote.StepsCompleted;

        var wasPatchedStepPreviouslyCompleted =
            (previousCompletedSteps & patchedStep) == patchedStep;

        // This PATCH means the customer has reviewed/submitted this step.
        quote.StepsDirty &= ~patchedStep;

        // If after patching, the step still does not satisfy its completion rule,
        // mark it dirty/incomplete again.
        if (!step.IsComplete(quote))
        {
            quote.StepsDirty |= patchedStep;
        }

        // Only invalidate downstream when editing an already-completed step.
        // Normal first-time forward progression must not make later steps stale.
        if (wasPatchedStepPreviouslyCompleted)
        {
            quoteStepInvalidationService.InvalidateStepsAfter(
                quote,
                patchedStep,
                previousCompletedSteps);
        }

        // Recalculate final completed state.
        quote.StepsCompleted = progressCalculator.CalculateCompletedSteps(quote);

        if ((quote.StepsCompleted & patchedStep) == patchedStep)
        {
            quote.LastCompletedStepKey = patchedStepKey;
        }
    }

    private static decimal CalculateUnitVolumeInCubicMeters(int height, int width, int depth) => (height * width * depth) / 1_000_000m;

    public static async Task ProcessExtraOptions(IAdditionalPriceRepository additionalPriceRepository,
        QuoteV2 quote, int numberOfItemsToDismantle, int numberOfItemsToAssemble, int numberOfSelectedVans,
        CancellationToken cancellationToken)
    {
        var unitDismantlePrice = await GetDismantleRate(AdditionalPriceType.Dismantle, additionalPriceRepository, cancellationToken);
        var unitAssemblePrice = await GetDismantleRate(AdditionalPriceType.Assembly, additionalPriceRepository, cancellationToken);

        quote.OptionalExtas = true;
        quote.NumberOfItemsToDismantle = numberOfItemsToDismantle;
        quote.NumberOfItemsToAssemble = numberOfItemsToAssemble;
        quote.SelectedVanCount = numberOfSelectedVans;

        var dismantleCost = numberOfItemsToDismantle * unitDismantlePrice;
        var assemblyCost = numberOfItemsToAssemble * unitAssemblePrice;
        var totalCost = dismantleCost + assemblyCost;

        var selectedPricing = quote.Pricings.First(p => p.IsSelected);
        selectedPricing.AssemblyCost = assemblyCost;
        selectedPricing.DismantleCost = dismantleCost;
        selectedPricing.SubtotalWithoutVat += totalCost;
        selectedPricing.TotalCostWithVat = selectedPricing.SubtotalWithoutVat + selectedPricing.SubtotalWithoutVat * VatRate;
    }
}
