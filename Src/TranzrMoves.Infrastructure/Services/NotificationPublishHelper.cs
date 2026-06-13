using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.Infrastructure.Services;

public static class NotificationPublishHelper
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
        Create(
            messageId,
            correlationId,
            NotificationCategory.Transactional,
            templateKey,
            toEmail,
            fromEmail,
            subject,
            templateData,
            textTemplateKey,
            bcc);

    public static SendNotification CreateMarketing(
        Guid messageId,
        string correlationId,
        string templateKey,
        string toEmail,
        string? fromEmail,
        string? subject,
        object templateData,
        string? textTemplateKey = null,
        IReadOnlyList<string>? bcc = null) =>
        Create(
            messageId,
            correlationId,
            NotificationCategory.Marketing,
            templateKey,
            toEmail,
            fromEmail,
            subject,
            templateData,
            textTemplateKey,
            bcc);

    private static SendNotification Create(
        Guid messageId,
        string correlationId,
        NotificationCategory category,
        string templateKey,
        string toEmail,
        string? fromEmail,
        string? subject,
        object templateData,
        string? textTemplateKey,
        IReadOnlyList<string>? bcc) =>
        new(
            messageId,
            correlationId,
            category,
            NotificationChannel.Email,
            templateKey,
            textTemplateKey,
            toEmail,
            fromEmail,
            subject,
            NotificationTemplateData.FromObject(templateData),
            bcc);
}
