namespace TranzrMoves.Notifications.Contracts;

public sealed record SendNotification(
    Guid MessageId,
    string CorrelationId,
    NotificationCategory Category,
    NotificationChannel Channel,
    string TemplateKey,
    string? TextTemplateKey,
    string ToEmail,
    string? FromEmail,
    string? Subject,
    IReadOnlyDictionary<string, object?> TemplateData,
    IReadOnlyList<string>? Bcc = null);
