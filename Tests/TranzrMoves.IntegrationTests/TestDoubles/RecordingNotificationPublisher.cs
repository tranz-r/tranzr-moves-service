using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.IntegrationTests.TestDoubles;

public sealed class RecordingNotificationPublisher : INotificationPublisher
{
    private readonly List<SendNotification> _published = [];
    private readonly object _lock = new();

    public IReadOnlyList<SendNotification> Published
    {
        get
        {
            lock (_lock)
            {
                return _published.ToList();
            }
        }
    }

    public Task PublishAsync(SendNotification message, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _published.Add(message);
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _published.Clear();
        }
    }
}
