// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Quote.Patch.Pricing;

public record PatchPricingStepCommand : ICommand<ErrorOr<QuoteJourneyResponse>>
{
    public Guid QuoteId { get; set; }

    public uint ExpectedVersion { get; set; }

    public Guid PricingId { get; set; }
    public int NumberOfSelectedVans { get; set; }
    public int NumberOfItemsToDismantle { get; set; }
    public int NumberOfItemsToAssemble { get; set; }
}
