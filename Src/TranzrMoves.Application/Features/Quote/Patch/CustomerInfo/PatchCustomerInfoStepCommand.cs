// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Quote.Patch.CustomerInfo;

public record PatchCustomerInfoStepCommand : ICommand<ErrorOr<QuoteJourneyResponse>>
{
    public Guid QuoteId { get; set; }

    public uint ExpectedVersion { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsBillingAddressSameAsOrigin { get; set; }
    public QuoteAddressDto? Address { get; set; }
}
