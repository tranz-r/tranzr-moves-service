using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Checkout.CollectPayLater;

public sealed class CollectQuoteV2PayLaterPaymentsCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteV2LaterBalanceCollectionService laterBalanceCollectionService,
    ITimeService timeService,
    ILogger<CollectQuoteV2PayLaterPaymentsCommandHandler> logger)
    : ICommandHandler<CollectQuoteV2PayLaterPaymentsCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(CollectQuoteV2PayLaterPaymentsCommand command,
        CancellationToken cancellationToken)
    {
        var quotes =
            await quoteRepository.GetPayLaterQuoteV2sForTodayAsync(timeService.TodayInUtc(), cancellationToken);

        foreach (var quote in quotes)
        {
            var result = await laterBalanceCollectionService.CollectAsync(quote, cancellationToken);
            if (result.IsError)
            {
                logger.LogWarning("Pay-later collection failed for QuoteV2 {QuoteId}: {Errors}", quote.Id,
                    result.Errors);
            }
        }

        return Result.Success;
    }
}
