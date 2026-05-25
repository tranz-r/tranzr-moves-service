using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Quote.Journey.Init;

public sealed class InitQuoteJourneyCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteResumeResolver resumeResolver,
    ILogger<InitQuoteJourneyCommandHandler> logger)
    : ICommandHandler<InitQuoteJourneyCommand, ErrorOr<QuoteJourneyResponse>>
{
    public async ValueTask<ErrorOr<QuoteJourneyResponse>> Handle(InitQuoteJourneyCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.GuestId))
        {
            return Error.Validation("Quote.GuestId.Required", "Guest ID is required.");
        }

        var quote = await quoteRepository.GetOrCreateQuoteV2Async(command.GuestId, command.QuoteType, cancellationToken);
        if (quote is null)
        {
            logger.LogError("Failed to initialize QuoteV2 journey for guest {GuestId} and type {QuoteType}.",
                command.GuestId, command.QuoteType);
            return Error.Failure("Quote.Init.Failed", "Could not initialize quote journey.");
        }

        var mapper = new QuoteMapper();
        return new QuoteJourneyResponse
        {
            Journey = resumeResolver.Resolve(quote),
            Quote = mapper.ToQuoteSnapshotDto(quote)
        };
    }
}
