using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using StackExchange.Redis;
using Stripe;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.IntegrationTests.Fixtures;
using TranzrMoves.IntegrationTests.Helpers;
namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

/// <summary>
/// Layer-by-layer diagnostics for PayLater E2E failures. Run individually to see where the pipeline breaks.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PayLaterEndToEndDiagnosticsTests(PayLaterEndToEndFixture fixture)
    : IClassFixture<PayLaterEndToEndFixture>
{
    [Fact]
    public async Task Layer1_RedisKeyExpiry_FiresKeyspaceNotification()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var quoteId = Guid.NewGuid();
        var key = PayLaterChargeKeys.ForQuote(quoteId);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscriber = fixture.Redis.GetSubscriber();
        await subscriber.SubscribeAsync(
            new RedisChannel("__keyevent@*__:expired", RedisChannel.PatternMode.Pattern),
            (_, message) => received.TrySetResult(message!));

        await fixture.Redis.GetDatabase().StringSetAsync(key, "test", TimeSpan.FromSeconds(1));

        var expiredKey = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        Assert.Equal(key, expiredKey);
    }

    [Fact]
    public async Task Layer2_DirectCollect_PersistsBalancePaymentIntent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var paymentMethodId = await fixture.EnsureStripePaymentMethodAsync(cancellationToken);
        var quoteId = await SeedQuoteAsync(paymentMethodId, cancellationToken);

        using var scope = fixture.CreateWorkerScope();
        var quoteRepository = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
        var loaded = await quoteRepository.GetQuoteByIdAsync(quoteId, cancellationToken, asTracking: true);
        Assert.NotNull(loaded);
        Assert.Equal(PaymentStatus.PaymentSetup, loaded!.PaymentStatus);
        var laterPm = loaded.Payments?.FirstOrDefault(p => p.PaymentType == PaymentType.Later)?.PaymentMethodId;
        Assert.False(string.IsNullOrEmpty(laterPm), "Later payment method missing on loaded quote.");

        var collectionService = scope.ServiceProvider.GetRequiredService<IQuoteV2LaterBalanceCollectionService>();
        var result = await collectionService.CollectAsync(loaded, cancellationToken);

        if (result.IsError)
        {
            Assert.Fail($"CollectAsync failed: {string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"))}");
        }

        var stripeClient = scope.ServiceProvider.GetRequiredService<StripeClient>();
        var customers = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{PayLaterStripeTestHelper.E2eCustomerEmail}'"
        }, cancellationToken: cancellationToken);

        var customerId = customers.Data.FirstOrDefault()?.Id;
        var recentIntents = customerId is null
            ? []
            : (await stripeClient.V1.PaymentIntents.ListAsync(new PaymentIntentListOptions
            {
                Customer = customerId,
                Limit = 5
            }, cancellationToken: cancellationToken)).Data;

        var stripeIntents = await stripeClient.V1.PaymentIntents.SearchAsync(new PaymentIntentSearchOptions
        {
            Query = $"metadata['QuoteId']:'{quoteId}'"
        }, cancellationToken: cancellationToken);

        await using var db = await fixture.CreateDbContextAsync(cancellationToken);
        var allPayments = await db.Set<Payment>().AsNoTracking()
            .Where(p => p.QuoteId == quoteId)
            .Select(p => new { p.PaymentType, p.PaymentIntentId, p.StripeSessionId, p.Status })
            .ToListAsync(cancellationToken);

        var balance = allPayments.FirstOrDefault(p => p.PaymentType == PaymentType.Balance);

        var latestIntent = recentIntents.FirstOrDefault();
        var latestMeta = latestIntent?.Metadata.GetValueOrDefault(nameof(PaymentMetadata.QuoteId)) ?? "(none)";

        Assert.False(string.IsNullOrEmpty(balance?.PaymentIntentId),
            $"CollectAsync succeeded but DB has no balance PaymentIntentId. Payments: " +
            $"{string.Join(", ", allPayments.Select(p => $"{p.PaymentType}/{p.Status}/pi={p.PaymentIntentId ?? "null"}/sess={p.StripeSessionId ?? "null"}"))}. " +
            $"Stripe PIs for quote: {string.Join(", ", stripeIntents.Data.Select(pi => $"{pi.Id}/{pi.Status}"))}. " +
            $"Recent customer PIs: {string.Join(", ", recentIntents.Select(pi => $"{pi.Id}/{pi.Status}"))}. " +
            $"Latest PI QuoteId metadata={latestMeta}, expected={quoteId}. " +
            "If latest PI matches, SaveChanges likely failed (check concurrency/xmin).");
    }

    [Fact]
    public async Task Layer3_DirectPublish_InvokesHandlerAndPersistsPaymentIntent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var paymentMethodId = await fixture.EnsureStripePaymentMethodAsync(cancellationToken);
        var quoteId = await SeedQuoteAsync(paymentMethodId, cancellationToken);
        var today = SystemClock.Instance.GetCurrentInstant().InUtc().Date;

        await fixture.PublishBalanceChargeAsync(new CollectQuoteV2BalanceCharge(quoteId, today), cancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            await using var db = await fixture.CreateDbContextAsync(cancellationToken);
            var balance = await db.Set<Payment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.QuoteId == quoteId && p.PaymentType == PaymentType.Balance, cancellationToken);

            if (!string.IsNullOrEmpty(balance?.PaymentIntentId))
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        Assert.Fail("Direct Wolverine publish did not produce a balance PaymentIntentId within 60s.");
    }

    private async Task<Guid> SeedQuoteAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        await using var db = await fixture.CreateDbContextAsync(cancellationToken);
        var now = SystemClock.Instance.GetCurrentInstant();
        var today = now.InUtc().Date;
        var quoteId = Guid.NewGuid();

        db.Set<QuoteV2>().Add(new QuoteV2
        {
            Id = quoteId,
            SessionId = $"diag-{quoteId:N}"[..16],
            Type = QuoteType.Removals,
            QuoteReference = $"DIAG-{quoteId:N}"[..18],
            VanType = VanType.largeVan,
            CrewCount = 1,
            TotalCost = 500m,
            PaymentStatus = PaymentStatus.PaymentSetup,
            CustomerId = fixture.SharedCustomerId,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "diag",
            ModifiedBy = "diag"
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            PaymentType = PaymentType.Later,
            PaymentMethodId = paymentMethodId,
            DueDate = today,
            Status = StripePaymentStatus.Paid,
            CustomerSelectedOption = true,
            SetupIntentId = "seti_diag",
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "diag",
            ModifiedBy = "diag"
        });

        await db.SaveChangesAsync(cancellationToken);
        return quoteId;
    }
}
