using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.IntegrationTests.TestDoubles;

/// <summary>
/// Runs balance collection in-process (same handler logic as production) without RabbitMQ.
/// Used by PayLater/Deposit E2E fixtures so Redis expiry → collection → DB is reliable in tests.
/// Rabbit wiring is covered by <see cref="Fixtures.PayLaterBalanceChargeMessagingFixture"/>.
/// </summary>
internal sealed class InProcessCollectQuoteV2BalanceChargePublisher(
    IQuoteRepository quoteRepository,
    IQuoteV2LaterBalanceCollectionService laterBalanceCollectionService,
    IQuoteV2DepositBalanceCollectionService depositBalanceCollectionService) : ICollectQuoteV2BalanceChargePublisher
{
    public async Task PublishAsync(CollectQuoteV2BalanceCharge message, CancellationToken cancellationToken = default)
    {
        var quote = await quoteRepository.GetQuoteByIdAsync(message.QuoteId, cancellationToken);
        if (quote is null)
        {
            return;
        }

        _ = quote.PaymentStatus switch
        {
            PaymentStatus.PaymentSetup => await laterBalanceCollectionService.TryCollectAsync(message.QuoteId,
                cancellationToken),
            PaymentStatus.PartiallyPaid => await depositBalanceCollectionService.TryCollectAsync(message.QuoteId,
                cancellationToken),
            _ => ErrorOr.Result.Success
        };
    }
}
