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

namespace TranzrMoves.Application.Features.Quote.Patch.MoveDateTime;

public class PatchMoveDateTimeStepCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteResumeResolver resumeResolver,
    IQuoteProgressCalculator progressCalculator,
    PricingContext pricingContext,
    IRemovalPricingRepository removalPricingRepository,
    IAdditionalPriceRepository additionalPriceRepository,
    IQuoteStepInvalidationService quoteStepInvalidationService,
    IQuoteJourneyProvider quoteJourneyProvider,
    IClock clock,
    ILogger<PatchMoveDateTimeStepCommandHandler> logger)
    : ICommandHandler<PatchMoveDateTimeStepCommand, ErrorOr<QuoteJourneyResponse>>
{
    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(
        PatchMoveDateTimeStepCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Patching move date and time");
        var quote = await quoteRepository.GetQuoteByIdAsync(command.QuoteId, cancellationToken, true);

        if (quote is null) return Error.NotFound("Quote not found");

        var versionCheck = QuoteV2Concurrency.EnsureExpectedVersion(quote, command.ExpectedVersion);
        if (versionCheck.IsError)
        {
            return versionCheck.Errors;
        }

        quote!.Schedule = command.Schedule;
        quote.SelectedVanCount = command.SelectedVanCount;

        await GeneratePricingAsync(quote, cancellationToken);

        PricingHelper.RecalculateStepState(quote!, QuoteSteps.MoveDateAndTimeSlot,
            QuoteStepKeys.MoveDateTime, quoteStepInvalidationService, progressCalculator,
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

    private async Task GeneratePricingAsync(QuoteV2 quote, CancellationToken cancellationToken)
    {
        var baseToOriginCost = RemovalQuoteEngine.CalculateBaseToOriginPrice(quote.BaseToOriginDistanceInMiles);
        await pricingContext.GenerateAsync(quote, baseToOriginCost, cancellationToken);
    }
}
