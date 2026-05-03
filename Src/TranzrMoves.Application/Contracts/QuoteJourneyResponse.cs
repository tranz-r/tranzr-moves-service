// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TranzrMoves.Application.Contracts;

public sealed class QuoteJourneyResponse
{
    public QuoteJourneyState Journey { get; init; } = default!;
    public QuoteSnapshotDto Quote { get; init; } = default!;
}
