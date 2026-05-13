// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IPricingStrategy
{
    bool CanHandle(QuoteType quoteType);
    Task Generate(QuoteV2 quote, decimal baseToOriginCost, CancellationToken cancellationToken);
    void SelectPricingOption(QuoteV2 quote, Guid pricingId);
    Task ExtraOption(QuoteV2 quote, int numberOfItemsToDismantle, int numberOfItemsToAssemble, int numberOfSelectedVans, CancellationToken cancellationToken);
}
