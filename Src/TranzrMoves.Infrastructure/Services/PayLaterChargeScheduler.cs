using System.Text.Json;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;
using StackExchange.Redis;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public sealed class PayLaterOptions
{
    public const string SectionName = "PayLater";

    public int RecoveryIntervalMinutes { get; set; } = 30;

    public bool UseDurableMessaging { get; set; } = true;
}

public sealed class PayLaterChargeScheduler(
    IConnectionMultiplexer redis,
    ITimeService timeService,
    ILogger<PayLaterChargeScheduler> logger) : IPayLaterChargeScheduler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly LocalDatePattern DueDatePattern = LocalDatePattern.Iso;

    public async Task ScheduleAsync(Guid quoteId, LocalDate dueDate, string quoteReference,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = PayLaterChargeKeys.ForQuote(quoteId);
        var payload = JsonSerializer.Serialize(new
        {
            quoteId,
            dueDate = DueDatePattern.Format(dueDate),
            quoteReference
        }, JsonOptions);
        var ttl = CalculateTtl(dueDate);

        var scheduled = await db.StringSetAsync(key, payload, ttl, When.NotExists);
        if (scheduled)
        {
            logger.LogInformation(
                "Scheduled pay-later balance charge for quote {QuoteId} at due date {DueDate} (TTL {TtlSeconds}s)",
                quoteId,
                dueDate,
                (long)ttl.TotalSeconds);
            return;
        }

        logger.LogDebug("Pay-later charge key already exists for quote {QuoteId}; skipping reschedule", quoteId);
    }

    private TimeSpan CalculateTtl(LocalDate dueDate)
    {
        var dueInstant = dueDate.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
        var remaining = dueInstant - timeService.Now();
        if (remaining <= Duration.Zero)
        {
            return TimeSpan.FromSeconds(1);
        }

        return remaining.ToTimeSpan();
    }
}
