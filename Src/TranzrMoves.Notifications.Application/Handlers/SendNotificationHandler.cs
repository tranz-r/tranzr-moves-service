using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using TranzrMoves.Notifications.Contracts;
using TranzrMoves.Notifications.Infrastructure;
using TranzrMoves.Notifications.Infrastructure.Entities;
using TranzrMoves.Notifications.Infrastructure.Interfaces;

namespace TranzrMoves.Notifications.Application.Handlers;

public sealed class SendNotificationHandler(
    NotificationsDbContext db,
    ITemplateService templateService,
    IEmailSender emailSender,
    IClock clock,
    ILogger<SendNotificationHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Handle(SendNotification message, CancellationToken cancellationToken)
    {
        if (message.Channel != NotificationChannel.Email)
        {
            logger.LogWarning("Unsupported channel {Channel} for message {MessageId}", message.Channel, message.MessageId);
            return;
        }

        var existing = await db.NotificationDeliveries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MessageId == message.MessageId, cancellationToken);

        if (existing?.Status == NotificationDeliveryStatus.Succeeded)
        {
            logger.LogInformation("Skipping duplicate notification {MessageId}", message.MessageId);
            return;
        }

        if (message.Category == NotificationCategory.Marketing)
        {
            logger.LogWarning(
                "Marketing notification {MessageId} skipped — consent tables not enabled (Phase 2)",
                message.MessageId);
            await RecordDeliveryAsync(message, NotificationDeliveryStatus.Skipped, null,
                "Marketing sends require Phase 2 consent", cancellationToken);
            return;
        }

        var delivery = existing ?? new NotificationDelivery
        {
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            TemplateKey = message.TemplateKey,
            ToEmail = message.ToEmail,
            Category = message.Category.ToString(),
            Status = NotificationDeliveryStatus.Pending,
            CreatedAt = clock.GetCurrentInstant()
        };

        if (existing is null)
        {
            db.NotificationDeliveries.Add(delivery);
            await db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            var templateData = ToTemplateObject(message.TemplateData);
            var htmlTemplate = message.TemplateKey.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                ? message.TemplateKey
                : $"{message.TemplateKey}.html";
            var textTemplate = message.TextTemplateKey
                               ?? (message.TemplateKey.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                                   ? message.TemplateKey.Replace(".html", ".txt", StringComparison.OrdinalIgnoreCase)
                                   : $"{message.TemplateKey}.txt");

            var html = templateService.GenerateEmail(htmlTemplate, templateData);
            var text = templateService.GenerateEmail(textTemplate, templateData);
            var from = message.FromEmail ?? "bookings@tranzrmoves.com";
            var subject = message.Subject
                          ?? $"Notification - {message.TemplateKey}";

            var providerId = await emailSender.SendAsync(
                from,
                subject,
                message.ToEmail,
                html,
                text,
                message.Bcc,
                cancellationToken);

            await RecordDeliveryAsync(message, NotificationDeliveryStatus.Succeeded, providerId, null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification {MessageId} to {Email}", message.MessageId,
                message.ToEmail);
            await RecordDeliveryAsync(message, NotificationDeliveryStatus.Failed, null, ex.Message,
                cancellationToken);
            throw;
        }
    }

    private async Task RecordDeliveryAsync(
        SendNotification message,
        NotificationDeliveryStatus status,
        string? providerMessageId,
        string? error,
        CancellationToken cancellationToken)
    {
        var row = await db.NotificationDeliveries
            .FirstOrDefaultAsync(x => x.MessageId == message.MessageId, cancellationToken);

        if (row is null)
        {
            row = new NotificationDelivery
            {
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId,
                TemplateKey = message.TemplateKey,
                ToEmail = message.ToEmail,
                Category = message.Category.ToString(),
                CreatedAt = clock.GetCurrentInstant()
            };
            db.NotificationDeliveries.Add(row);
        }

        row.Status = status;
        row.ProviderMessageId = providerMessageId;
        row.Error = error;
        row.CompletedAt = clock.GetCurrentInstant();
        await db.SaveChangesAsync(cancellationToken);
    }

    private static object ToTemplateObject(IReadOnlyDictionary<string, object?> data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions) ?? new Dictionary<string, object?>();
    }
}
