using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NodaTime;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.Worker.HostedServices;

public sealed class QuoteReminderWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<QuoteRemindersOptions> options,
    ILogger<QuoteReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Quote reminder worker is disabled");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.ScanIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Quote reminder worker failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task PublishDueRemindersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var quoteRepository = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
        var notificationPublisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();
        var resumeResolver = scope.ServiceProvider.GetRequiredService<IQuoteResumeResolver>();
        var resumeTokenService = scope.ServiceProvider.GetRequiredService<IQuoteResumeTokenService>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var reminderOptions = options.Value;

        var now = clock.GetCurrentInstant();
        var idleBefore = now.Minus(Duration.FromHours(reminderOptions.IdleHoursBeforeReminder));
        var cooldownBefore = now.Minus(Duration.FromDays(reminderOptions.ReminderCooldownDays));

        var quotes = await quoteRepository.GetQuotesDueForReminderAsync(
            idleBefore,
            cooldownBefore,
            now,
            cancellationToken);

        var published = 0;

        foreach (var quote in quotes)
        {
            if (quote.Customer?.Email is not { Length: > 0 } email)
            {
                continue;
            }

            var journey = resumeResolver.Resolve(quote);
            if (!journey.IsResumable || string.IsNullOrWhiteSpace(journey.ResumeUrl))
            {
                continue;
            }

            var resumeToken = resumeTokenService.Create(
                quote,
                TimeSpan.FromDays(reminderOptions.ResumeTokenLifetimeDays));
            var resumeUrl = BuildResumeUrl(reminderOptions.FrontendBaseUrl, journey.ResumeUrl, resumeToken);

            var customerName = $"{quote.Customer.FirstName?.Trim()} {quote.Customer.LastName?.Trim()}".Trim();
            if (string.IsNullOrWhiteSpace(customerName))
            {
                customerName = email;
            }

            var messageId = CreateReminderMessageId(quote.Id, now, reminderOptions.ReminderCooldownDays);
            var templateData = new
            {
                customerName,
                quoteReference = quote.QuoteReference,
                resumeUrl,
                currentYear = now.InUtc().Year
            };

            await notificationPublisher.PublishAsync(
                NotificationPublishHelper.Create(
                    messageId,
                    quote.Id.ToString(),
                    "quote-reminder",
                    email,
                    FromEmails.Booking,
                    $"Finish your quote #{quote.QuoteReference}",
                    templateData),
                cancellationToken);

            quote.LastResumeEmailSentAt = now;
            var saveResult = await quoteRepository.SaveChangesAsync(cancellationToken);
            if (saveResult.IsError)
            {
                logger.LogWarning(
                    "Failed to persist LastResumeEmailSentAt for quote {QuoteId}: {Error}",
                    quote.Id,
                    saveResult.FirstError.Description);
                continue;
            }

            published++;
        }

        if (published > 0)
        {
            logger.LogInformation("Quote reminder worker published {Count} reminders", published);
        }
    }

    public static Guid CreateReminderMessageId(Guid quoteId, Instant now, int cooldownDays)
    {
        var daysSinceEpoch = now.ToUnixTimeSeconds() / 86_400;
        var cooldownWindow = daysSinceEpoch / Math.Max(1, cooldownDays);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{quoteId:N}:{cooldownWindow}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static string BuildResumeUrl(string frontendBaseUrl, string resumePath, string token)
    {
        var baseUrl = frontendBaseUrl.TrimEnd('/');
        var path = resumePath.StartsWith('/') ? resumePath : $"/{resumePath}";
        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }
}
