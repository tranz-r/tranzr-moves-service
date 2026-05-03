using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common;
using TranzrMoves.Application.Common.Strategy;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Application.Services;
using TranzrMoves.Application.Statics;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Quote.Patch.Pricing;

public class PatchPricingStepCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteResumeResolver resumeResolver,
    IQuoteProgressCalculator progressCalculator,
    PricingContext pricingContext,
    IRemovalPricingRepository removalPricingRepository,
    IAdditionalPriceRepository additionalPriceRepository,
    IClock clock,
    ILogger<PatchPricingStepCommandHandler> logger)
    : ICommandHandler<PatchPricingStepCommand, ErrorOr<QuoteJourneyResponse>>
{
    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(
        PatchPricingStepCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Patching pricing step");
        var quote = await quoteRepository.GetQuoteByIdAsync(command.QuoteId, cancellationToken, true);

        if (quote == null)
        {
            logger.LogInformation("No quote found for {QuoteId}", command.QuoteId);
            return Error.NotFound($"No quote found for id {command.QuoteId}");
        }

        var versionCheck = QuoteV2Concurrency.EnsureExpectedVersion(quote, command.ExpectedVersion);
        if (versionCheck.IsError)
        {
            return versionCheck.Errors;
        }

        if (quote.OriginToDestinationDistanceInMiles is null)
            throw new InvalidOperationException("Distance must be calculated before pricing.");

        if (quote.InventoryItems.Count == 0)
            throw new InvalidOperationException("Inventory items are required before pricing.");

        try
        {
            await CalculatePrice(quote, command.PricingId, command.NumberOfItemsToAssemble, command.NumberOfItemsToDismantle, command.NumberOfSelectedVans, cancellationToken);
        }
        catch (Exception e)
        {
            return Error.Failure(e.Message);
        }

        var (stepFlag, stepKey) = quote.Type == QuoteType.Removals
            ? (QuoteSteps.RemovalPricing, QuoteStepKeys.RemovalPricing)
            : (QuoteSteps.Pricing, QuoteStepKeys.Pricing);

        RecalculateStepState(quote, stepFlag, stepKey);

        var saveResult = await quoteRepository.SaveChangesAsync(cancellationToken);
        if (saveResult.IsError)
        {
            return saveResult.Errors;
        }

        var (standardTexts, premiumTexts, additionalServices)
            = await PricingHelper.GetAdditionalServicesAndServiceTextsAsync(clock,
                removalPricingRepository,
                additionalPriceRepository,
                cancellationToken);

        var mapper = new QuoteMapper();
        var quoteSnapShot = mapper.ToQuoteSnapshotDto(quote);
        quoteSnapShot.StandardServiceTexts = standardTexts;
        quoteSnapShot.PremiumServiceTexts = premiumTexts;
        quoteSnapShot.AdditionalServices = additionalServices;

        return new QuoteJourneyResponse
        {
            Journey = resumeResolver.Resolve(quote),
            Quote = quoteSnapShot
        };
    }

    private async Task CalculatePrice(QuoteV2 quote, Guid pricingId, int numberOfItemsToAssemble, int numberOfItemsToDismantle, int numberOfSelectedVans, CancellationToken cancellationToken)
    {
        await pricingContext.SelectPricingOption(quote!, pricingId, numberOfItemsToDismantle, numberOfItemsToAssemble, numberOfSelectedVans, cancellationToken);
    }

    private void RecalculateStepState(QuoteV2 quote, QuoteSteps justPatchedStep, string justPatchedStepKey)
    {
        quote.StepsCompleted = progressCalculator.CalculateCompletedSteps(quote);

        if ((quote.StepsCompleted & justPatchedStep) == justPatchedStep)
        {
            quote.LastCompletedStepKey = justPatchedStepKey;
        }
    }
}
