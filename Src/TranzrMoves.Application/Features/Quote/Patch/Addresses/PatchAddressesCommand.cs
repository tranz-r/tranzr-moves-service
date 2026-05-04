// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Quote.Patch.Addresses;

public record PatchAddressesCommand : ICommand<ErrorOr<QuoteJourneyResponse>>
{
    public Guid QuoteId { get; set; }

    /// <summary>Row version from the last <see cref="QuoteSnapshotDto.Version"/> (send as If-Match).</summary>
    public uint ExpectedVersion { get; set; }

    public List<QuoteAddressDto> Addresses { get; set; } = [];
}
