// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Services;

public sealed class QuoteProgressCalculator(
    IQuoteJourneyProvider journeyProvider) : IQuoteProgressCalculator
{
    public QuoteSteps CalculateCompletedSteps(QuoteV2 quote)
    {
        var journey = journeyProvider.Get(quote.Type);

        var completed = QuoteSteps.None;

        foreach (var step in journey.Steps)
        {
            var isDirty = (quote.StepsDirty & step.Flag) == step.Flag;

            if (isDirty || !step.IsComplete(quote))
                break;

            completed |= step.Flag;
        }

        return completed;
    }
}
