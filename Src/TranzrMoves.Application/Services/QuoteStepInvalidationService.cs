// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Services;

public interface IQuoteStepInvalidationService
{
    void InvalidateStepsAfter(
        QuoteV2 quote,
        QuoteSteps changedStep,
        QuoteSteps previousCompletedSteps);

    void ClearDirtyStep(QuoteV2 quote, QuoteSteps completedStep);
}

public sealed class QuoteStepInvalidationService(
    IQuoteJourneyProvider journeyProvider) : IQuoteStepInvalidationService
{
    public void InvalidateStepsAfter(
        QuoteV2 quote,
        QuoteSteps changedStep,
        QuoteSteps previousCompletedSteps)
    {
        var journey = journeyProvider.Get(quote.Type);

        var changedIndex = journey.Steps.FindIndex(x => x.Flag == changedStep);
        if (changedIndex < 0)
            return;

        var downstream = journey.Steps
            .Skip(changedIndex + 1)
            .Aggregate(QuoteSteps.None, (current, step) => current | step.Flag);

        // Only downstream steps that were completed BEFORE this patch
        // should become stale.
        var previouslyCompletedDownstreamSteps = downstream & previousCompletedSteps;

        quote.StepsDirty |= previouslyCompletedDownstreamSteps;
        quote.StepsCompleted &= ~previouslyCompletedDownstreamSteps;

        if ((previouslyCompletedDownstreamSteps & QuoteSteps.QuoteSummary) == QuoteSteps.QuoteSummary)
            quote.SummaryConfirmedAt = null;
    }

    public void ClearDirtyStep(QuoteV2 quote, QuoteSteps completedStep)
    {
        quote.StepsDirty &= ~completedStep;
    }
}
