using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;
using StackExchange.Redis;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public sealed class BalanceChargeScheduler(
    IConnectionMultiplexer redis,
    ITimeService timeService,
    IOptions<PayLaterOptions> options,
    ILogger<BalanceChargeScheduler> logger) : IBalanceChargeScheduler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly LocalDatePattern DueDatePattern = LocalDatePattern.Iso;

    public async Task SchedulePayLaterAsync(Guid quoteId, LocalDate dueDate, string quoteReference,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = PayLaterChargeKeys.ForQuote(quoteId);
        var payload = JsonSerializer.Serialize(new
        {
            quoteId,
            dueDate = DueDatePattern.Format(dueDate),
            quoteReference,
            kind = "paylater"
        }, JsonOptions);

        var dueInstant = dueDate.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
        await SetKeyIfNotExistsAsync(db, key, payload, dueInstant, quoteId, "pay-later", cancellationToken);
    }

    public async Task ScheduleDepositBalanceAsync(Guid quoteId, LocalDate collectionDate, string quoteReference,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = DepositBalanceChargeKeys.ForQuote(quoteId);
        var timeZoneId = options.Value.DepositChargeTimeZone ?? BalanceChargeScheduling.DefaultDepositChargeTimeZoneId;
        var payload = JsonSerializer.Serialize(new
        {
            quoteId,
            collectionDate = DueDatePattern.Format(collectionDate),
            quoteReference,
            kind = "deposit"
        }, JsonOptions);

        var chargeInstant = BalanceChargeScheduling.GetDepositChargeInstant(collectionDate, timeZoneId);
        await SetKeyIfNotExistsAsync(db, key, payload, chargeInstant, quoteId, "deposit balance", cancellationToken);
    }

    private async Task SetKeyIfNotExistsAsync(
        IDatabase db,
        string key,
        string payload,
        Instant chargeInstant,
        Guid quoteId,
        string label,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ttl = CalculateTtl(chargeInstant);

        var scheduled = await db.StringSetAsync(key, payload, ttl, When.NotExists);
        if (scheduled)
        {
            logger.LogInformation(
                "Scheduled {Label} charge for quote {QuoteId} at {ChargeInstant} (TTL {TtlSeconds}s)",
                label,
                quoteId,
                chargeInstant,
                (long)ttl.TotalSeconds);
            return;
        }

        logger.LogDebug("{Label} charge key already exists for quote {QuoteId}; skipping reschedule", label, quoteId);
    }

    private TimeSpan CalculateTtl(Instant chargeInstant)
    {
        var remaining = chargeInstant - timeService.Now();
        if (remaining <= Duration.Zero)
        {
            return TimeSpan.FromSeconds(1);
        }

        return remaining.ToTimeSpan();
    }
}
