using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.Infrastructure.Services;

internal static class NotificationPublishHelper
{
    public static SendNotification Create(
        Guid messageId,
        string correlationId,
        string templateKey,
        string toEmail,
        string? fromEmail,
        string? subject,
        object templateData,
        string? textTemplateKey = null,
        IReadOnlyList<string>? bcc = null) =>
        new(
            messageId,
            correlationId,
            NotificationCategory.Transactional,
            NotificationChannel.Email,
            templateKey,
            textTemplateKey,
            toEmail,
            fromEmail,
            subject,
            NotificationTemplateData.FromObject(templateData),
            bcc);
}
