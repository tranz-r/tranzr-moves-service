using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Stripe;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Infrastructure;
using TranzrMoves.IntegrationTests.Fixtures;
using TranzrMoves.IntegrationTests.Helpers;

namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

public class PayLaterV2Tests(TestServerFixture fixture) : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;

    [Fact]
    public async Task V2_Send_Checkout_Session_When_PayLater_PaymentIntent_Fails_3DS()
    {
        TranzrMovesDbContext? dbContext = fixture.DbContext;
        var (quoteId, dueDate) = await CreatePayLaterQuoteV2(dbContext!, "pm_card_authenticationRequired");

        await TriggerPayLaterCollectionAsync(quoteId, dueDate, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task V2_Send_Checkout_Session_When_Card_Has_Insufficient_Fund()
    {
        TranzrMovesDbContext? dbContext = fixture.DbContext;
        var (quoteId, dueDate) = await CreatePayLaterQuoteV2(
            dbContext!,
            PayLaterStripeTestHelper.InsufficientFundsPaymentMethodId);

        await TriggerPayLaterCollectionAsync(quoteId, dueDate, TestContext.Current.CancellationToken);
    }

    private async Task TriggerPayLaterCollectionAsync(Guid quoteId, LocalDate dueDate, CancellationToken cancellationToken)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<ICollectQuoteV2BalanceChargePublisher>();
        var act = () => publisher.PublishAsync(new CollectQuoteV2BalanceCharge(quoteId, dueDate), cancellationToken);
        await act.Should().NotThrowAsync();
    }

    private async Task<(Guid QuoteId, LocalDate DueDate)> CreatePayLaterQuoteV2(
        TranzrMovesDbContext dbContext,
        string paymentMethodId)
    {
        dbContext.ChangeTracker.Clear();

        var now = SystemClock.Instance.GetCurrentInstant();
        var today = now.InUtc().Date;

        var userId = Guid.NewGuid();
        var user = new UserV2
        {
            Id = userId,
            Email = PayLaterStripeTestHelper.IntegrationTestCustomerEmail,
            FirstName = "Int",
            LastName = "Test",
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        };

        dbContext.Set<UserV2>().Add(user);

        var quoteId = Guid.NewGuid();
        var quote = new QuoteV2
        {
            Id = quoteId,
            SessionId = $"v2pl-{quoteId:N}"[..16],
            Type = QuoteType.Removals,
            QuoteReference = $"V2-{quoteId:N}"[..18],
            VanType = VanType.largeVan,
            CrewCount = 1,
            TotalCost = 500m,
            PaymentStatus = PaymentStatus.PaymentSetup,
            CustomerId = userId,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        };

        dbContext.Set<QuoteV2>().Add(quote);

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            CollectionDate = today.PlusDays(10).AtStartOfDayInZone(DateTimeZone.Utc).ToInstant(),
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        };

        dbContext.Set<Schedule>().Add(schedule);

        var stripeClient = fixture.Services.GetRequiredService<StripeClient>();
        var stripeCustomer = await PayLaterStripeTestHelper.EnsureGbCustomerAsync(
            stripeClient,
            PayLaterStripeTestHelper.IntegrationTestCustomerEmail,
            "Tranzr Integration Test",
            cancellationToken: TestContext.Current.CancellationToken);

        // Decline tokens fail if attached; charge-time failure is exercised on balance collection.
        var storedPaymentMethodId = paymentMethodId == PayLaterStripeTestHelper.InsufficientFundsPaymentMethodId
            ? paymentMethodId
            : await PayLaterStripeTestHelper.AttachTestPaymentMethodAsync(
                stripeClient,
                stripeCustomer.Id,
                paymentMethodId,
                TestContext.Current.CancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            PaymentType = PaymentType.Later,
            PaymentMethodId = storedPaymentMethodId,
            DueDate = today,
            Status = StripePaymentStatus.Paid,
            CustomerSelectedOption = true,
            SetupIntentId = "seti_test_placeholder",
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "test",
            ModifiedBy = "test"
        };

        dbContext.Set<Payment>().Add(payment);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (quoteId, today);
    }

    public ValueTask InitializeAsync() => new(Task.CompletedTask);

    public async ValueTask DisposeAsync() => await _resetDatabase();
}
