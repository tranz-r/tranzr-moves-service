using Microsoft.Extensions.Options;
using NodaTime;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Services;

namespace TranzrMoves.Worker.HostedServices;

public sealed class BalanceChargeRecoveryWorker(
    IServiceScopeFactory scopeFactory,
    ITimeService timeService,
    IOptions<PayLaterOptions> options,
    ILogger<BalanceChargeRecoveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.RecoveryIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecoverDueChargesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Balance charge recovery worker failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RecoverDueChargesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var quoteRepository = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<ICollectQuoteV2BalanceChargePublisher>();

        var timeZoneId = options.Value.DepositChargeTimeZone ?? BalanceChargeScheduling.DefaultDepositChargeTimeZoneId;
        var now = timeService.Now();
        var todayUtc = timeService.TodayInUtc();
        var todayLondon = timeService.TodayIn(timeZoneId);

        var payLaterPublished = await PublishPayLaterRecoveriesAsync(quoteRepository, publisher, todayUtc, cancellationToken);
        var depositPublished =
            await PublishDepositRecoveriesAsync(quoteRepository, publisher, todayLondon, now, timeZoneId, cancellationToken);

        logger.LogInformation(
            "Balance charge recovery published pay-later {PayLaterCount} and deposit {DepositCount} quotes",
            payLaterPublished,
            depositPublished);
    }

    private static async Task<int> PublishPayLaterRecoveriesAsync(
        IQuoteRepository quoteRepository,
        ICollectQuoteV2BalanceChargePublisher publisher,
        LocalDate todayUtc,
        CancellationToken cancellationToken)
    {
        var quotes = await quoteRepository.GetPayLaterQuoteV2sDueForCollectionAsync(todayUtc, cancellationToken);
        var published = 0;

        foreach (var quote in quotes)
        {
            var laterPayment = quote.Payments?
                .Where(p => p.PaymentType == PaymentType.Later && p.DueDate is not null)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (laterPayment?.DueDate is null)
            {
                continue;
            }

            await publisher.PublishAsync(
                new CollectQuoteV2BalanceCharge(quote.Id, laterPayment.DueDate.Value),
                cancellationToken);
            published++;
        }

        return published;
    }

    private static async Task<int> PublishDepositRecoveriesAsync(
        IQuoteRepository quoteRepository,
        ICollectQuoteV2BalanceChargePublisher publisher,
        LocalDate todayLondon,
        Instant now,
        string timeZoneId,
        CancellationToken cancellationToken)
    {
        var quotes = await quoteRepository.GetDepositQuoteV2sDueForBalanceCollectionAsync(todayLondon, cancellationToken);
        var published = 0;

        foreach (var quote in quotes)
        {
            var depositPayment = quote.Payments?
                .Where(p => p.PaymentType == PaymentType.Deposit && p.DueDate is not null)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (depositPayment?.DueDate is not { } collectionDate)
            {
                continue;
            }

            if (!BalanceChargeScheduling.IsDepositChargeDue(collectionDate, now, timeZoneId))
            {
                continue;
            }

            await publisher.PublishAsync(
                new CollectQuoteV2BalanceCharge(quote.Id, collectionDate),
                cancellationToken);
            published++;
        }

        return published;
    }
}
