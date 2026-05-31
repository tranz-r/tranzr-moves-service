using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Checkout.CollectPayLater;

public sealed class CollectQuoteV2BalanceChargeHandler(
    IQuoteV2LaterBalanceCollectionService collectionService,
    ILogger<CollectQuoteV2BalanceChargeHandler> logger)
{
    public async Task Handle(CollectQuoteV2BalanceCharge message, CancellationToken cancellationToken)
    {
        var result = await collectionService.TryCollectAsync(message.QuoteId, cancellationToken);
        if (result.IsError)
        {
            logger.LogWarning(
                "Pay-later balance collection failed for QuoteV2 {QuoteId}: {Errors}",
                message.QuoteId,
                result.Errors);
        }
    }
}
