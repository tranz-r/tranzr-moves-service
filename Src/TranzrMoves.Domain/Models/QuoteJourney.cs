// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Models;

public sealed class QuoteJourney
{
    public required QuoteType QuoteType { get; init; }
    public required IReadOnlyList<QuoteJourneyStep> Steps { get; init; }
}
