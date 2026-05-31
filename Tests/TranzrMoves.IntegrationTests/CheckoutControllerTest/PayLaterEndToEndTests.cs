using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Stripe;
using TranzrMoves.Domain.Entities;
using TranzrMoves.IntegrationTests.Fixtures;

namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

[Trait("Category", "Integration")]
public sealed class PayLaterEndToEndTests(PayLaterEndToEndFixture fixture)
    : IClassFixture<PayLaterEndToEndFixture>
{
    [Fact]
    public async Task RedisExpiry_PublishesToRabbitMq_ProcessorCollectsAndPersistsPaymentIntent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var paymentMethodId = await fixture.EnsureStripePaymentMethodAsync(cancellationToken);
        var quoteId = await SeedPayLaterQuoteAsync(paymentMethodId, cancellationToken);

        await fixture.ExpirePayLaterKeyAsync(quoteId);

        var balancePayment = await WaitForBalancePaymentAsync(quoteId, cancellationToken);
        await AssertSucceededPaymentIntentAsync(balancePayment, cancellationToken);
    }

    private async Task AssertSucceededPaymentIntentAsync(Payment balancePayment, CancellationToken cancellationToken)
    {
        Assert.False(string.IsNullOrEmpty(balancePayment.PaymentIntentId));
        Assert.StartsWith("pi_", balancePayment.PaymentIntentId, StringComparison.Ordinal);

        using var scope = fixture.CreateWorkerScope();
        var stripeClient = scope.ServiceProvider.GetRequiredService<StripeClient>();
        var paymentIntent = await stripeClient.V1.PaymentIntents.GetAsync(
            balancePayment.PaymentIntentId,
            cancellationToken: cancellationToken);

        Assert.Equal("succeeded", paymentIntent.Status);
    }

    private async Task<Payment> WaitForBalancePaymentAsync(Guid quoteId, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(120);
        Payment? balancePayment = null;

        while (DateTime.UtcNow < deadline)
        {
            await using var db = await fixture.CreateDbContextAsync(cancellationToken);
            balancePayment = await db.Set<Payment>()
                .AsNoTracking()
                .Where(p => p.QuoteId == quoteId && p.PaymentType == PaymentType.Balance)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrEmpty(balancePayment?.PaymentIntentId))
            {
                return balancePayment;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        await using var countDb = await fixture.CreateDbContextAsync(cancellationToken);
        var paymentCount = await countDb.Set<Payment>().CountAsync(cancellationToken);

        Assert.Fail(
            $"Timed out waiting for balance PaymentIntentId. Payments in DB: {paymentCount}.");
        return balancePayment!;
    }

    private async Task<Guid> SeedPayLaterQuoteAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        await using var db = await fixture.CreateDbContextAsync(cancellationToken);

        var now = SystemClock.Instance.GetCurrentInstant();
        var today = now.InUtc().Date;
        var quoteId = Guid.NewGuid();

        db.Set<QuoteV2>().Add(new QuoteV2
        {
            Id = quoteId,
            SessionId = $"e2e-{quoteId:N}"[..16],
            Type = QuoteType.Removals,
            QuoteReference = $"E2E-{quoteId:N}"[..18],
            VanType = VanType.largeVan,
            CrewCount = 1,
            TotalCost = 500m,
            PaymentStatus = PaymentStatus.PaymentSetup,
            CustomerId = fixture.SharedCustomerId,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "e2e-test",
            ModifiedBy = "e2e-test"
        });

        db.Set<Schedule>().Add(new Schedule
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            CollectionDate = today.PlusDays(10).AtStartOfDayInZone(DateTimeZone.Utc).ToInstant(),
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "e2e-test",
            ModifiedBy = "e2e-test"
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
            SetupIntentId = "seti_e2e_test",
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "e2e-test",
            ModifiedBy = "e2e-test"
        });

        await db.SaveChangesAsync(cancellationToken);
        return quoteId;
    }
}
