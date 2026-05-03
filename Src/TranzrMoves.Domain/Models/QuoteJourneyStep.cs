// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Models;

public sealed record QuoteJourneyStep(
    string Key,
    string Route,
    QuoteSteps Flag,
    bool Required,
    Func<QuoteV2, bool> IsComplete);
