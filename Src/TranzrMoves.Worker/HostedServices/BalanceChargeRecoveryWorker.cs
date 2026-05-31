using Microsoft.Extensions.Options;
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
                logger.LogError(ex, "Pay-later balance charge recovery worker failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RecoverDueChargesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var quoteRepository = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<ICollectQuoteV2BalanceChargePublisher>();

        var today = timeService.TodayInUtc();
        var quotes = await quoteRepository.GetPayLaterQuoteV2sDueForCollectionAsync(today, cancellationToken);
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

        logger.LogInformation(
            "Pay-later balance charge recovery published {PublishedCount} of {CandidateCount} due quotes for {DueDate}",
            published,
            quotes.Count,
            today);
    }
}
