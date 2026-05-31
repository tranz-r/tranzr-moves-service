using NodaTime;
using StackExchange.Redis;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Worker.HostedServices;

public sealed class PayLaterChargeExpiryListener(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    ILogger<PayLaterChargeExpiryListener> logger) : BackgroundService
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
            if (!key.StartsWith(PayLaterChargeKeys.Prefix, StringComparison.Ordinal))
            {
                return;
            }

            if (!Guid.TryParse(key[PayLaterChargeKeys.Prefix.Length..], out var quoteId))
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
                                  .Where(p => p.PaymentType == PaymentType.Later && p.DueDate is not null)
                                  .OrderByDescending(p => p.CreatedAt)
                                  .FirstOrDefault()
                                  ?.DueDate
                              ?? SystemClock.Instance.GetCurrentInstant().InUtc().Date;

                await publisher.PublishAsync(new CollectQuoteV2BalanceCharge(quoteId, dueDate), stoppingToken);
                logger.LogInformation(
                    "Published pay-later balance charge for quote {QuoteId} from Redis expiry",
                    quoteId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to publish pay-later balance charge for quote {QuoteId}", quoteId);
            }
        });

        logger.LogInformation("Subscribed to Redis key expiry events for pay-later balance charges");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            await subscriber.UnsubscribeAsync(channel);
        }
    }
}
