// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TranzrMoves.Application.Common.Strategy;

public sealed record DistancePricingBand(
    decimal FromInclusive,
    decimal ToExclusive,
    decimal RatePerMile);

public static class RemovalQuoteEngine
{
    private const decimal MaxBillableDistance = 1000m;

    private static readonly IReadOnlyList<DistancePricingBand> Bands =
    [
        new(0m,    50m,   0.00m),
        new(50m,   100m,  0.65m),
        new(100m,  200m,  0.90m),
        new(200m,  300m,  1.20m),
        new(300m,  500m,  1.50m),
        new(500m,  750m,  1.80m),
        new(750m,  1000m, 2.10m),
    ];

    public static decimal CalculateBaseToOriginPrice(long? distanceMiles)
    {
        if (distanceMiles < 0)
            throw new ArgumentOutOfRangeException(nameof(distanceMiles));

        // 🚨 cap distance
        var effectiveDistance = Math.Min(distanceMiles!.Value, MaxBillableDistance);

        decimal total = 0m;

        foreach (var band in Bands)
        {
            if (effectiveDistance <= band.FromInclusive)
                break;

            var milesInBand =
                Math.Min(effectiveDistance, band.ToExclusive) - band.FromInclusive;

            if (milesInBand <= 0)
                continue;

            total += milesInBand * band.RatePerMile;
        }

        return total;
    }
}
