using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.IntegrationTests.TestDoubles;

/// <summary>
/// Runs balance collection in-process (same handler logic as production) without RabbitMQ.
/// Used by <see cref="Fixtures.PayLaterEndToEndFixture"/> so Redis expiry → collection → DB is reliable in tests.
/// Rabbit wiring is covered by <see cref="Fixtures.PayLaterBalanceChargeMessagingFixture"/>.
/// </summary>
internal sealed class InProcessCollectQuoteV2BalanceChargePublisher(
    IQuoteV2LaterBalanceCollectionService collectionService) : ICollectQuoteV2BalanceChargePublisher
{
    public async Task PublishAsync(CollectQuoteV2BalanceCharge message, CancellationToken cancellationToken = default)
    {
        _ = await collectionService.TryCollectAsync(message.QuoteId, cancellationToken);
    }
}
