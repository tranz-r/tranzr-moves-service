using NodaTime;
using StackExchange.Redis;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Worker.HostedServices;

public sealed class BalanceChargeExpiryListener(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    ILogger<BalanceChargeExpiryListener> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();
        var channel = new RedisChannel("__keyevent@*__:expired", RedisChannel.PatternMode.Pattern);

        await subscriber.SubscribeAsync(channel, async (_, message) =>
        {
            if (message.IsNullOrEmpty)
            {
                return;
            }

            var key = message.ToString();
            if (key.StartsWith(PayLaterChargeKeys.Prefix, StringComparison.Ordinal))
            {
                await PublishFromExpiredKeyAsync(key, PayLaterChargeKeys.Prefix.Length, PaymentType.Later, stoppingToken);
                return;
            }

            if (key.StartsWith(DepositBalanceChargeKeys.Prefix, StringComparison.Ordinal))
            {
                await PublishFromExpiredKeyAsync(key, DepositBalanceChargeKeys.Prefix.Length, PaymentType.Deposit,
                    stoppingToken);
            }
        });

        logger.LogInformation("Subscribed to Redis key expiry events for balance charges (pay-later and deposit)");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            await subscriber.UnsubscribeAsync(channel);
        }
    }

    private async Task PublishFromExpiredKeyAsync(
        string key,
        int prefixLength,
        PaymentType paymentTypeForDueDate,
        CancellationToken stoppingToken)
    {
        if (!Guid.TryParse(key[prefixLength..], out var quoteId))
        {
            logger.LogWarning("Could not parse quote id from expired Redis key {RedisKey}", key);
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var quoteRepository = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
            var publisher = scope.ServiceProvider.GetRequiredService<ICollectQuoteV2BalanceChargePublisher>();

            var quote = await quoteRepository.GetQuoteByIdAsync(quoteId, stoppingToken);
            var dueDate = quote?.Payments?
                              .Where(p => p.PaymentType == paymentTypeForDueDate && p.DueDate is not null)
                              .OrderByDescending(p => p.CreatedAt)
                              .FirstOrDefault()
                              ?.DueDate
                          ?? quote?.Schedule?.CollectionDate?.InUtc().Date
                          ?? SystemClock.Instance.GetCurrentInstant().InUtc().Date;

            await publisher.PublishAsync(new CollectQuoteV2BalanceCharge(quoteId, dueDate), stoppingToken);
            logger.LogInformation(
                "Published balance charge for quote {QuoteId} from Redis expiry ({PaymentType})",
                quoteId,
                paymentTypeForDueDate);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to publish balance charge for quote {QuoteId}", quoteId);
        }
    }
}
