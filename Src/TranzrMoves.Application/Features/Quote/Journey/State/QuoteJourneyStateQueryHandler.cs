using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.Strategy;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Quote.Journey.State;

public class QuoteJourneyStateQueryHandler(
    IQuoteRepository quoteRepository,
    IQuoteResumeResolver resumeResolver,
    IRemovalPricingRepository removalPricingRepository,
    IAdditionalPriceRepository additionalPriceRepository,
    IClock clock,
    ILogger<QuoteJourneyStateQueryHandler> logger)
    : IQueryHandler<QuoteJourneyStateQuery, ErrorOr<QuoteJourneyResponse>>
{
    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(
        QuoteJourneyStateQuery command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting quote journey state");
        var quote = await quoteRepository.GetQuoteByIdAsync(command.QuoteId, cancellationToken, true);

        if (quote == null)
        {
            logger.LogInformation("No quote found for {QuoteId}", command.QuoteId);
            return Error.NotFound($"No quote found for id {command.QuoteId}");
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
}
