using NodaTime;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.IntegrationTests.TestDoubles;

public sealed class NoOpPayLaterChargeScheduler : IPayLaterChargeScheduler
{
    public Task ScheduleAsync(Guid quoteId, LocalDate dueDate, string quoteReference,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class DirectCollectQuoteV2BalanceChargePublisher(
    IQuoteV2LaterBalanceCollectionService collectionService) : ICollectQuoteV2BalanceChargePublisher
{
    public async Task PublishAsync(CollectQuoteV2BalanceCharge message, CancellationToken cancellationToken = default)
    {
        _ = await collectionService.TryCollectAsync(message.QuoteId, cancellationToken);
    }
}
