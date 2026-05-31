namespace TranzrMoves.Application.Messaging;

public interface ICollectQuoteV2BalanceChargePublisher
{
    Task PublishAsync(CollectQuoteV2BalanceCharge message, CancellationToken cancellationToken = default);
}
