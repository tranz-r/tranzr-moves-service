using NodaTime;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.IntegrationTests.TestDoubles;

public sealed class NoOpBalanceChargeScheduler : IBalanceChargeScheduler
{
    public Task SchedulePayLaterAsync(Guid quoteId, LocalDate dueDate, string quoteReference,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ScheduleDepositBalanceAsync(Guid quoteId, LocalDate collectionDate, string quoteReference,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class DirectCollectQuoteV2BalanceChargePublisher(
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
