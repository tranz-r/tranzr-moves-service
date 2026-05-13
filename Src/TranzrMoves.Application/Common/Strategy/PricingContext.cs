// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Common.Strategy;

public class PricingContext(IPricingStrategyResolver strategyResolver)
{
    public async Task GenerateAsync(QuoteV2 quote, decimal baseToOriginCost, CancellationToken cancellationToken)
    {
        var strategy = strategyResolver.Resolve(quote.Type);
        await strategy.Generate(quote, baseToOriginCost, cancellationToken);
    }

    public void SelectPricingOption(QuoteV2 quote, Guid pricingId)
    {
        var strategy = strategyResolver.Resolve(quote.Type);
        strategy.SelectPricingOption(quote, pricingId);
    }

    public async Task ExtraOption(QuoteV2 quote, int numberOfItemsToDismantle, int numberOfItemsToAssemble,
        int numberOfSelectedVans, CancellationToken cancellationToken)
    {
        var strategy = strategyResolver.Resolve(quote.Type);
        await strategy.ExtraOption(quote, numberOfItemsToDismantle, numberOfItemsToAssemble, numberOfSelectedVans, cancellationToken);
    }
}
