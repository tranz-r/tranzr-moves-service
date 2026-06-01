using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Checkout.CollectPayLater;

public sealed class CollectQuoteV2BalanceChargeHandler(
    IQuoteRepository quoteRepository,
    IQuoteV2LaterBalanceCollectionService laterBalanceCollectionService,
    IQuoteV2DepositBalanceCollectionService depositBalanceCollectionService,
    ILogger<CollectQuoteV2BalanceChargeHandler> logger)
{
    public async Task Handle(CollectQuoteV2BalanceCharge message, CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetQuoteByIdAsync(message.QuoteId, cancellationToken);
        if (quote is null)
        {
            logger.LogWarning("QuoteV2 {QuoteId} not found for balance charge", message.QuoteId);
            return;
        }

        var result = quote.PaymentStatus switch
        {
            PaymentStatus.PaymentSetup => await laterBalanceCollectionService.TryCollectAsync(message.QuoteId,
                cancellationToken),
            PaymentStatus.PartiallyPaid => await depositBalanceCollectionService.TryCollectAsync(message.QuoteId,
                cancellationToken),
            _ => LogSkipAndSucceed(quote)
        };

        if (result.IsError)
        {
            logger.LogWarning(
                "Balance collection failed for QuoteV2 {QuoteId} ({Status}): {Errors}",
                message.QuoteId,
                quote.PaymentStatus,
                result.Errors);
        }
    }

    private ErrorOr<Success> LogSkipAndSucceed(QuoteV2 quote)
    {
        logger.LogInformation(
            "Skipping balance collection for {QuoteRef}; status is {Status}",
            quote.QuoteReference,
            quote.PaymentStatus);
        return Result.Success;
    }
}
