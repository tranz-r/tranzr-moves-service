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

namespace TranzrMoves.Application.Features.Quote.Patch.Payment;

public class PatchPaymentStepCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteResumeResolver resumeResolver,
    IQuoteProgressCalculator progressCalculator,
    IRemovalPricingRepository removalPricingRepository,
    IAdditionalPriceRepository additionalPriceRepository,
    IQuoteStepInvalidationService quoteStepInvalidationService,
    IQuoteJourneyProvider quoteJourneyProvider,
    IClock clock,
    ILogger<PatchPaymentStepCommandHandler> logger)
    : ICommandHandler<PatchPaymentStepCommand, ErrorOr<QuoteJourneyResponse>>
{
    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(
        PatchPaymentStepCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Patching move date and time");
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

        var mapper = new QuoteMapper();

        PricingHelper.RecalculateStepState(quote!, QuoteSteps.Payment, QuoteStepKeys.Payment,
            quoteStepInvalidationService, progressCalculator,
            quoteJourneyProvider);

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
}
