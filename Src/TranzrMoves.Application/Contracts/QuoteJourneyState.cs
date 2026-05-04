// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public sealed class QuoteJourneyState
{
    public Guid QuoteId { get; init; }
    public string QuoteType { get; init; } = string.Empty;
    public bool IsResumable { get; init; }
    public string? ResumeStepKey { get; init; }
    public string? ResumeUrl { get; init; }
    public string? LastCompletedStepKey { get; init; }
    public IReadOnlyList<string> CompletedSteps { get; init; } = [];
    public IReadOnlyList<QuoteStepStateDto> Steps { get; init; } = [];
    public string? ReasonIfNotResumable { get; init; }

    public static QuoteJourneyState Resumable(
        Guid quoteId,
        QuoteType quoteType,
        string resumeStepKey,
        string resumeUrl,
        string? lastCompletedStepKey,
        IReadOnlyList<string> completedSteps,
        IReadOnlyList<QuoteStepStateDto> steps)
        => new()
        {
            QuoteId = quoteId,
            QuoteType = quoteType.ToString(),
            IsResumable = true,
            ResumeStepKey = resumeStepKey,
            ResumeUrl = resumeUrl,
            LastCompletedStepKey = lastCompletedStepKey,
            CompletedSteps = completedSteps,
            Steps = steps
        };

    public static QuoteJourneyState NotResumable(
        Guid quoteId,
        QuoteType quoteType,
        string reason)
        => new()
        {
            QuoteId = quoteId,
            QuoteType = quoteType.ToString(),
            IsResumable = false,
            ReasonIfNotResumable = reason
        };
}

public sealed class QuoteStepStateDto
{
    public string Key { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // complete/current/locked
}
