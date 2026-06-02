using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.Domain.Interfaces;

public interface INotificationPublisher
{
    Task PublishAsync(SendNotification message, CancellationToken cancellationToken = default);
}
