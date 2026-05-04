// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Common.Strategy;

public sealed class RemovalPricingStrategy(
    IClock clock,
    IAdditionalPriceRepository additionalPriceRepository,
    IRateCardRepository rateCardRepository) : IPricingStrategy
{
    public bool CanHandle(QuoteType quoteType) => quoteType == QuoteType.Removals;

    public async Task Generate(QuoteV2 quote, decimal baseToOriginCost,
        CancellationToken cancellationToken)
    {
        _ = rateCardRepository;
        await RecalculatePrices(quote, baseToOriginCost, cancellationToken);
    }

    public async Task SelectPricingOption(QuoteV2 quote, Guid pricingId, int numberOfItemsToDismantle, int numberOfItemsToAssemble, int numberOfSelectedVans,
        CancellationToken cancellationToken)
    {
        await PricingHelper.ProcessSelectedOptions(clock, additionalPriceRepository, quote, pricingId, numberOfItemsToDismantle, numberOfItemsToAssemble, numberOfSelectedVans, cancellationToken);
    }

    private async Task RecalculatePrices(QuoteV2 quote, decimal baseToOriginCost, CancellationToken cancellationToken)
    {
        if (quote.OriginToDestinationDistanceInMiles is null)
            throw new InvalidOperationException("Distance must be calculated before pricing.");

        if (quote.InventoryItems.Count == 0)
            throw new InvalidOperationException("Inventory items are required before pricing.");

        var now = clock.GetCurrentInstant();

        if (quote.SelectedVanCount <= 0)
            throw new InvalidOperationException("Selected vans must be greater than zero.");

        var capacityResult = PricingHelper.EvaluateVanCapacity(
            quote.TotalInventoryVolumeM3!.Value,
            quote.RecommendedVanCount!.Value,
            quote.SelectedVanCount!.Value,
            quote.EffectiveVanCapacityM3!.Value,
            quote.InventoryItems);

        quote.VanCapacityStatus = capacityResult.Status;
        quote.VanCapacityWarning = capacityResult.Warning;
        quote.PriceCalculatedAt = now;

        var rateCards = await rateCardRepository.GetRateCardsAsync(true, cancellationToken);

        quote.Pricings.Clear();

        foreach (var crew in new[] { 1, 2, 3 })
        {
            quote.Pricings.Add(await CreatePricingOption(
                quote,
                crew,
                ServiceLevel.Standard,
                baseToOriginCost,
                PricingHelper.VatRate,
                PricingHelper.FloorSurcharge,
                rateCards,
                cancellationToken));

            quote.Pricings.Add(await CreatePricingOption(
                quote,
                crew,
                ServiceLevel.Premium,
                baseToOriginCost,
                PricingHelper.VatRate,
                PricingHelper.FloorSurcharge,
                rateCards,
                cancellationToken));
        }
    }

    private async Task<Pricing> CreatePricingOption(
        QuoteV2 quote,
        int crew,
        ServiceLevel tier,
        decimal baseToOriginCost,
        decimal vatRate,
        decimal floorSurcharge,
        IList<RateCard> rateCards,
        CancellationToken cancellationToken)
    {
        var origin = quote!.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Origin);
        var dest = quote!.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Destination);

        if (origin is null || dest is null)
        {
            throw new InvalidEnumArgumentException();
        }

        var unitDismantlePrice = await PricingHelper.GetDismantleRate(AdditionalPriceType.Dismantle, additionalPriceRepository, cancellationToken);
        var unitAssemblePrice = await PricingHelper.GetDismantleRate(AdditionalPriceType.Assembly, additionalPriceRepository, cancellationToken);

        var dismantleCost = quote.NumberOfItemsToDismantle * unitDismantlePrice;
        var assemblyCost = quote.NumberOfItemsToAssemble * unitAssemblePrice;

        var originFloor = PricingHelper.GetFloor(quote, QuoteAddressKind.Origin);
        var destinationFloor = PricingHelper.GetFloor(quote, QuoteAddressKind.Destination);

        var hasNoElevatorAndIsNotGroundFloorAtOrigin = origin is { HasElevator: not true, Floor: > 0 };
        var hasNoElevatorAndIsNotGroundFloorADestination = dest is { HasElevator: not true, Floor: > 0 };

        var originFloorSurcharge = PricingHelper.CalculateFloorSurcharge(originFloor, floorSurcharge, hasNoElevatorAndIsNotGroundFloorAtOrigin);
        var destinationFloorSurcharge = PricingHelper.CalculateFloorSurcharge(destinationFloor, floorSurcharge, hasNoElevatorAndIsNotGroundFloorADestination);

        var specialHandlingCost = quote.InventoryItems
            .Where(x => PricingHelper.IsSpecialHandlingItem(x))
            .Sum(x => x.Quantity * 12m);

        var commercialSubtotal =
            dismantleCost +
            assemblyCost +
            originFloorSurcharge +
            destinationFloorSurcharge +
            specialHandlingCost;

        var congestionCharge = 0m; // Add later when you detect congestion zone
        var ulezCharge = 0m;       // Add later when you detect ULEZ
        var insuranceUplift = 0m;  // Add later if customer selects extra cover

        var rateCard = rateCards.First(x => x is { IsActive: true } && x.ServiceLevel == tier && x.Movers == crew);


        var subtotalWithoutVat =
            commercialSubtotal +
            congestionCharge +
            ulezCharge +
            insuranceUplift +
            baseToOriginCost +
            rateCard.BaseBlockPrice;

        var vatAmount = subtotalWithoutVat * vatRate;
        var totalWithVat = subtotalWithoutVat + vatAmount;

        return new Pricing
        {
            QuoteId = quote.Id,
            QuoteType = quote.Type,
            ServiceLevel = tier,

            SelectedVanCount = quote.SelectedVanCount!.Value,
            CrewCount = crew,
            CallOutCharge = rateCard.BaseBlockPrice + baseToOriginCost,
            NumberOfItemsToDismantle = quote!.NumberOfItemsToDismantle,
            NumberOfItemsToAssemble = quote!.NumberOfItemsToAssemble,

            BaseVanCost = 0m,
            DistanceCost = 0m,
            LabourCost = 0m,

            DismantleCost = PricingHelper.Round(dismantleCost),
            AssemblyCost = PricingHelper.Round(assemblyCost),
            OriginFloorSurcharge = PricingHelper.Round(originFloorSurcharge),
            DestinationFloorSurcharge = PricingHelper.Round(destinationFloorSurcharge),
            SpecialHandlingCost = PricingHelper.Round(specialHandlingCost!.Value),
            UlezSurcharge = PricingHelper.Round(ulezCharge),
            CongestionZoneSurcharge = PricingHelper.Round(congestionCharge),
            InsuranceUplift = PricingHelper.Round(insuranceUplift),

            Vat = 20,
            SubtotalWithoutVat = PricingHelper.Round(subtotalWithoutVat!.Value),
            VatAmount = PricingHelper.Round(vatAmount!.Value),
            TotalCostWithVat = PricingHelper.Round(totalWithVat!.Value),

            IsSelected = false
        };
    }
}
