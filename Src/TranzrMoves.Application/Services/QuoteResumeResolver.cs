// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Services;

public interface IQuoteResumeResolver
{
    QuoteJourneyState Resolve(QuoteV2 quote);
}

public sealed class QuoteResumeResolver : IQuoteResumeResolver
{
    private readonly IQuoteJourneyProvider _journeyProvider;
    private readonly IQuoteProgressCalculator _progressCalculator;
    private readonly IClock _clock;

    public QuoteResumeResolver(
        IQuoteJourneyProvider journeyProvider,
        IQuoteProgressCalculator progressCalculator,
        IClock clock)
    {
        _journeyProvider = journeyProvider;
        _progressCalculator = progressCalculator;
        _clock = clock;
    }

    public QuoteJourneyState Resolve(QuoteV2 quote)
    {
        var now = _clock.GetCurrentInstant();

        if (quote.ExpiresAt is not null && quote.ExpiresAt.Value < now)
        {
            return QuoteJourneyState.NotResumable(
                quote.Id,
                quote.Type,
                "Quote has expired.");
        }

        var journey = _journeyProvider.Get(quote.Type);

        quote.StepsCompleted = _progressCalculator.CalculateCompletedSteps(quote);

        var firstIncomplete = journey.Steps
            .FirstOrDefault(x => x.Required && !IsEffectivelyComplete(quote, x));

        var resumeStep = firstIncomplete ?? journey.Steps.Last();

        var steps = journey.Steps
            .Select(step => new QuoteStepStateDto
            {
                Key = step.Key,
                Route = step.Route,
                Status = BuildStatus(step, resumeStep, quote)
            })
            .ToArray();

        var completedSteps = journey.Steps
            .Where(step => (quote.StepsCompleted & step.Flag) == step.Flag)
            .Select(step => step.Key)
            .ToArray();

        return QuoteJourneyState.Resumable(
            quote.Id,
            quote.Type,
            resumeStep.Key,
            resumeStep.Route,
            quote.LastCompletedStepKey,
            completedSteps,
            steps);
    }

    private static bool IsEffectivelyComplete(QuoteV2 quote, QuoteJourneyStep step)
    {
        var isDirty = (quote.StepsDirty & step.Flag) == step.Flag;
        var isComplete = step.IsComplete(quote);

        return isComplete && !isDirty;
    }

    private static string BuildStatus(QuoteJourneyStep step, QuoteJourneyStep resumeStep, QuoteV2 quote)
    {
        if (IsEffectivelyComplete(quote, step))
            return "complete";

        if (step.Key == resumeStep.Key)
            return "current";

        if ((quote.StepsDirty & step.Flag) == step.Flag)
            return "stale";

        return "locked";
    }
}
