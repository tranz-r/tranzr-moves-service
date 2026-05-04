// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TranzrMoves.Application.Statics;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Services;

public sealed class QuoteJourneyProvider : IQuoteJourneyProvider
{
    public QuoteJourney Get(QuoteType quoteType) =>
        quoteType switch
        {
            QuoteType.Send => BuildSendOrReceiveJourneySteps(QuoteType.Send),
            QuoteType.Receive => BuildSendOrReceiveJourneySteps(QuoteType.Receive),
            QuoteType.Removals => BuildRemovalsJourney(),
            _ => throw new ArgumentOutOfRangeException(nameof(quoteType))
        };

    private static QuoteJourney BuildSendOrReceiveJourneySteps(QuoteType quoteType) =>
        new()
        {
            QuoteType = quoteType,
            Steps =
            [
                new(
                    QuoteStepKeys.CollectionDeliveryAddresses,
                    "/collection-delivery",
                    QuoteSteps.CollectionDeliveryAddresses,
                    true,
                    QuoteCompletionRules.HasCompletedAddressesForSend),

                new(
                    QuoteStepKeys.Inventory,
                    "/inventory",
                    QuoteSteps.Inventory,
                    true,
                    QuoteCompletionRules.HasCompletedInventory),

                new(
                    QuoteStepKeys.MoveDateTime,
                    "/van-and-date",
                    QuoteSteps.MoveDateAndTimeSlot,
                    true,
                    QuoteCompletionRules.HasCompletedSchedule),

                new(
                    QuoteStepKeys.Pricing,
                    "/pricing",
                    QuoteSteps.Pricing,
                    true,
                    QuoteCompletionRules.HasCompletedPricing),

                new(
                    QuoteStepKeys.CustomerInfo,
                    "/origin-destination",
                    QuoteSteps.CustomerInfo,
                    true,
                    QuoteCompletionRules.HasCompletedCustomerInfo),

                new(
                    QuoteStepKeys.QuoteSummary,
                    "/summary",
                    QuoteSteps.QuoteSummary,
                    true,
                    QuoteCompletionRules.HasCompletedQuoteSummary),

                new(
                    QuoteStepKeys.Payment,
                    "/pay",
                    QuoteSteps.Payment,
                    true,
                    QuoteCompletionRules.HasCompletedPayment),

                new(
                    QuoteStepKeys.Complete,
                    "/confirmation",
                    QuoteSteps.Complete,
                    true,
                    QuoteCompletionRules.HasCompletedPayment)
            ]
        };

    private static QuoteJourney BuildRemovalsJourney() =>
        new()
        {
            QuoteType = QuoteType.Removals,
            Steps =
            [
                new(
                    QuoteStepKeys.CollectionDeliveryAddresses,
                    "/collection-delivery",
                    QuoteSteps.CollectionDeliveryAddresses,
                    true,
                    QuoteCompletionRules.HasCompletedAddressesForRemovals),

                new(
                    QuoteStepKeys.Inventory,
                    "/inventory",
                    QuoteSteps.Inventory,
                    true,
                    QuoteCompletionRules.HasCompletedInventory),

                new(
                    QuoteStepKeys.MoveDateTime,
                    "/van-and-date",
                    QuoteSteps.MoveDateAndTimeSlot,
                    true,
                    QuoteCompletionRules.HasCompletedSchedule),

                new(
                    QuoteStepKeys.RemovalPricing,
                    "/removal-pricing",
                    QuoteSteps.RemovalPricing,
                    true,
                    QuoteCompletionRules.HasCompletedPricing),

                new(
                    QuoteStepKeys.CustomerInfo,
                    "/origin-destination",
                    QuoteSteps.CustomerInfo,
                    true,
                    QuoteCompletionRules.HasCompletedCustomerInfo),

                new(
                    QuoteStepKeys.QuoteSummary,
                    "/summary",
                    QuoteSteps.QuoteSummary,
                    true,
                    QuoteCompletionRules.HasCompletedQuoteSummary),

                new(
                    QuoteStepKeys.Payment,
                    "/pay",
                    QuoteSteps.Payment,
                    true,
                    QuoteCompletionRules.HasCompletedPayment),

                new(
                    QuoteStepKeys.Complete,
                    "/confirmation",
                    QuoteSteps.Complete,
                    true,
                    QuoteCompletionRules.HasCompletedPayment)
            ]
        };
}
