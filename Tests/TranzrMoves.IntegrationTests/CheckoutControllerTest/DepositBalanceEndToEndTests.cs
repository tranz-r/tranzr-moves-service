using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Stripe;
using TranzrMoves.Domain.Entities;
using TranzrMoves.IntegrationTests.Fixtures;
using TranzrMoves.IntegrationTests.Helpers;

namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

[Trait("Category", "Integration")]
public sealed class DepositBalanceEndToEndTests(PayLaterEndToEndFixture fixture)
    : IClassFixture<PayLaterEndToEndFixture>
{
    [Fact]
    public async Task RedisExpiry_PublishesAndCollectsDepositBalance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var paymentMethodId = await fixture.EnsureStripePaymentMethodAsync(cancellationToken);
        var quoteId = await SeedDepositQuoteAsync(paymentMethodId, cancellationToken);

        await fixture.ExpireDepositBalanceKeyAsync(quoteId);

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

        Assert.Fail("Timed out waiting for deposit balance PaymentIntentId.");
        return balancePayment!;
    }

    private async Task<Guid> SeedDepositQuoteAsync(string paymentMethodId, CancellationToken cancellationToken)
    {
        await using var db = await fixture.CreateDbContextAsync(cancellationToken);

        var now = SystemClock.Instance.GetCurrentInstant();
        var today = now.InUtc().Date;
        var quoteId = Guid.NewGuid();

        db.Set<QuoteV2>().Add(new QuoteV2
        {
            Id = quoteId,
            SessionId = $"dep-e2e-{Guid.NewGuid():N}"[..14],
            Type = QuoteType.Removals,
            QuoteReference = $"DE2E-{Guid.NewGuid():N}"[..12],
            VanType = VanType.largeVan,
            CrewCount = 1,
            TotalCost = 400m,
            PaymentStatus = PaymentStatus.PartiallyPaid,
            CustomerId = fixture.SharedCustomerId,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "e2e",
            ModifiedBy = "e2e"
        });

        db.Set<Schedule>().Add(new Schedule
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            CollectionDate = today.PlusDays(10).AtStartOfDayInZone(DateTimeZone.Utc).ToInstant(),
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "e2e",
            ModifiedBy = "e2e"
        });

        db.Set<Payment>().Add(new Payment
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            PaymentType = PaymentType.Deposit,
            Status = StripePaymentStatus.Paid,
            PaymentMethodId = paymentMethodId,
            Amount = 100m,
            DueDate = today.PlusDays(10),
            CustomerSelectedOption = true,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "e2e",
            ModifiedBy = "e2e"
        });

        await db.SaveChangesAsync(cancellationToken);
        return quoteId;
    }
}
