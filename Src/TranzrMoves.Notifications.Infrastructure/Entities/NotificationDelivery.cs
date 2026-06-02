using NodaTime;

namespace TranzrMoves.Notifications.Infrastructure.Entities;

public sealed class NotificationDelivery
{
    public Guid MessageId { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public string TemplateKey { get; set; } = string.Empty;

    public string ToEmail { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public NotificationDeliveryStatus Status { get; set; }

    public string? ProviderMessageId { get; set; }

    public string? Error { get; set; }

    public Instant CreatedAt { get; set; }

    public Instant? CompletedAt { get; set; }
}
