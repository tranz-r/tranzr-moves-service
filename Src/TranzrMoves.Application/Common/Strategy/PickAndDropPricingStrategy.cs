// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Common.Strategy;

public sealed class PickAndDropPricingStrategy(IClock clock, IAdditionalPriceRepository additionalPriceRepository) : IPricingStrategy
{
    private const decimal BaseVanFee = 65m;
    public bool CanHandle(QuoteType quoteType) => quoteType is QuoteType.Send or QuoteType.Receive;

    public async Task Generate(QuoteV2 quote, decimal baseToOriginCost, CancellationToken cancellationToken)
    {
        await RecalculatePrices(quote, baseToOriginCost, cancellationToken);
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
                now,
                cancellationToken));

            quote.Pricings.Add(await CreatePricingOption(
                quote,
                crew,
                ServiceLevel.Premium,
                baseToOriginCost,
                PricingHelper.VatRate,
                PricingHelper.FloorSurcharge,
                now,
                cancellationToken));
        }
    }

    public async Task SelectPricingOption(QuoteV2 quote, Guid pricingId, int numberOfItemsToDismantle, int numberOfItemsToAssemble, int numberOfSelectedVans,
        CancellationToken cancellationToken)
    {
        await PricingHelper.ProcessSelectedOptions(clock, additionalPriceRepository, quote, pricingId, numberOfItemsToDismantle, numberOfItemsToAssemble, numberOfSelectedVans, cancellationToken);
    }

    private async Task<Pricing> CreatePricingOption(
        QuoteV2 quote,
        int crew,
        ServiceLevel tier,
        decimal baseToOriginCost,
        decimal vatRate,
        decimal floorSurcharge,
        Instant now,
        CancellationToken cancellationToken)
    {
        var origin = quote!.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Origin);
        var dest = quote!.Addresses.FirstOrDefault(x => x.Kind == QuoteAddressKind.Destination);

        if (origin is null || dest is null)
        {
            throw new InvalidEnumArgumentException();
        }

        var miles = quote.OriginToDestinationDistanceInMiles!.Value;

        var vans = quote.SelectedVanCount;
        var totalVolume = quote.TotalInventoryVolumeM3;

        var baseVanCost = vans * BaseVanFee;

        var distanceCost = vans * CalculateDistanceCost(miles);

        var estimatedHours = EstimateJobHours(
            totalVolume!.Value,
            miles,
            crew,
            vans!.Value);

        var labourCost = vans * GetCrewHourlyRate(crew) * estimatedHours;

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

        var callOutCharge = CalculateCallOutCharge(quote.BaseToOriginDistanceInMiles);

        var commercialSubtotal =
            baseVanCost +
            distanceCost +
            labourCost +
            dismantleCost +
            assemblyCost +
            originFloorSurcharge +
            destinationFloorSurcharge +
            specialHandlingCost +
            callOutCharge;

        var tierMultiplier = tier == ServiceLevel.Premium ? 1.25m : 1.00m;

        var tierAdjustedSubtotal = commercialSubtotal * tierMultiplier;

        var congestionCharge = 0m; // Add later when you detect congestion zone
        var ulezCharge = 0m;       // Add later when you detect ULEZ
        var insuranceUplift = 0m;  // Add later if customer selects extra cover

        var subtotalWithoutVat =
            tierAdjustedSubtotal +
            congestionCharge +
            ulezCharge +
            insuranceUplift +
            baseToOriginCost;

        var vatAmount = subtotalWithoutVat * vatRate;
        var totalWithVat = subtotalWithoutVat + vatAmount;

        return new Pricing
        {
            //Id = Guid.NewGuid(), //TODO: Is this needed ?
            QuoteId = quote.Id,
            QuoteType = quote.Type,
            ServiceLevel = tier,

            SelectedVanCount = vans.Value,
            CrewCount = crew,
            BaseToOriginCost = PricingHelper.Round(baseToOriginCost),
            BaseVanCost = PricingHelper.Round(baseVanCost!.Value),
            DistanceCost = PricingHelper.Round(distanceCost!.Value),
            LabourCost = PricingHelper.Round(labourCost.Value),

            DismantleCost = PricingHelper.Round(dismantleCost),
            AssemblyCost = PricingHelper.Round(assemblyCost),

            OriginFloorSurcharge = PricingHelper.Round(originFloorSurcharge),
            DestinationFloorSurcharge = PricingHelper.Round(destinationFloorSurcharge),

            SpecialHandlingCost = PricingHelper.Round(specialHandlingCost!.Value),

            CallOutCharge = PricingHelper.Round(callOutCharge),
            UlezSurcharge = PricingHelper.Round(ulezCharge),
            CongestionZoneSurcharge = PricingHelper.Round(congestionCharge),
            InsuranceUplift = PricingHelper.Round(insuranceUplift),

            Vat = 20,
            VatRate = vatRate,
            SubtotalWithoutVat = PricingHelper.Round(subtotalWithoutVat!.Value),
            VatAmount = PricingHelper.Round(vatAmount!.Value),
            TotalCostWithVat = PricingHelper.Round(totalWithVat!.Value),

            IsSelected = false,

            //TODO: Revisit these
            CreatedAt = now,
            ModifiedAt = now
        };
    }

    private static decimal CalculateDistanceCost(decimal miles)
    {
        var cappedMiles = Math.Min(miles, 1000m);

        var bands = new[]
        {
            new DistanceBand(0m, 50m, 0m),
            new DistanceBand(50m, 100m, 0.65m),
            new DistanceBand(100m, 200m, 1.00m),
            new DistanceBand(200m, 300m, 1.50m),
            new DistanceBand(300m, 500m, 1.85m),
            new DistanceBand(500m, 750m, 2.15m),
            new DistanceBand(750m, 1000m, 2.40m)
        };

        var total = 0m;

        foreach (var band in bands)
        {
            if (cappedMiles <= band.From)
                continue;

            var chargeableMiles = Math.Min(cappedMiles, band.To) - band.From;

            total += chargeableMiles * band.RatePerMile;
        }

        return total;
    }

    private static decimal EstimateJobHours(
        decimal totalVolumeM3,
        decimal miles,
        int crew,
        int vans)
    {
        var volumePerVan = totalVolumeM3 / vans;

        var loadingHours = volumePerVan / GetCrewThroughputM3PerHour(crew);

        var travelHours = EstimateTravelHours(miles);

        var setupTime = 0.65m;

        return setupTime + loadingHours + travelHours;
    }

    private static decimal EstimateTravelHours(decimal miles)
    {
        var averageSpeed = miles switch
        {
            <= 10m => 18m,
            <= 30m => 25m,
            <= 100m => 35m,
            _ => 45m
        };

        return miles / averageSpeed;
    }

    private static decimal GetCrewThroughputM3PerHour(int crew)
    {
        return crew switch
        {
            1 => 2.20m,
            2 => 4.50m,
            3 => 6.50m,
            _ => throw new ArgumentOutOfRangeException(nameof(crew))
        };
    }

    //TODO: Revisit this as well
    private static decimal GetCrewHourlyRate(int crew)
    {
        return crew switch
        {
            1 => 35m,
            2 => 55m,
            3 => 75m,
            _ => throw new ArgumentOutOfRangeException(nameof(crew))
        };
    }

    private static decimal CalculateCallOutCharge(long? baseToOriginMiles)
    {
        if (baseToOriginMiles is null or <= 10)
            return 0m;

        return (baseToOriginMiles.Value - 10) * 0.75m;
    }

    private sealed record DistanceBand(decimal From, decimal To, decimal RatePerMile);
}
