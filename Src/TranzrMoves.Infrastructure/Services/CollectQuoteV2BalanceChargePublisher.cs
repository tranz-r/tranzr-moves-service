using TranzrMoves.Application.Messaging;
using Wolverine;

namespace TranzrMoves.Infrastructure.Services;

public sealed class CollectQuoteV2BalanceChargePublisher(IMessageBus messageBus) : ICollectQuoteV2BalanceChargePublisher
{
    public async Task PublishAsync(CollectQuoteV2BalanceCharge message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await messageBus.PublishAsync(message);
    }
}
