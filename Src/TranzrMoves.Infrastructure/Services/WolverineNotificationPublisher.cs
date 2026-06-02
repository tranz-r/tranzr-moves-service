using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Notifications.Contracts;
using Wolverine;

namespace TranzrMoves.Infrastructure.Services;

public sealed class WolverineNotificationPublisher(IMessageBus messageBus) : INotificationPublisher
{
    public async Task PublishAsync(SendNotification message, CancellationToken cancellationToken = default)
    {
        await messageBus.PublishAsync(message);
    }
}
