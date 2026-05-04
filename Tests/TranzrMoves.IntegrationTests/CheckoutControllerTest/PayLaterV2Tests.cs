using System.Net;
using FluentAssertions;
using NodaTime;
using Stripe;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Infrastructure;

namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

public class PayLaterV2Tests(TestServerFixture fixture) : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;
    private HttpClient Client => fixture.CreateClient();

    [Fact]
    public async Task V2_Send_Checkout_Session_When_PayLater_PaymentIntent_Fails_3DS()
    {
        TranzrMovesDbContext? dbContext = fixture.DbContext;
        await CreatePayLaterQuoteV2(dbContext!, "pm_card_authenticationRequired");

        var response = await Client.PostAsync("/api/v2/checkout/pay-later-collection", null,
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task V2_Send_Checkout_Session_When_Card_Has_Insufficient_Fund()
    {
        TranzrMovesDbContext? dbContext = fixture.DbContext;
        await CreatePayLaterQuoteV2(dbContext!, "pm_card_visa_chargeDeclinedInsufficientFunds");

        var response = await Client.PostAsync("/api/v2/checkout/pay-later-collection", null,
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task CreatePayLaterQuoteV2(TranzrMovesDbContext dbContext, string paymentMethodId)
    {
        dbContext.ChangeTracker.Clear();

        var now = SystemClock.Instance.GetCurrentInstant();
        var today = now.InUtc().Date;

        var userId = Guid.NewGuid();
        var user = new UserV2
        {
            Id = userId,
            Email = "int-test@tranzrmoves.com",
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

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            PaymentType = PaymentType.Later,
            PaymentMethodId = paymentMethodId,
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

        await CreateIntegrationTestCustomerOnStripe(TestContext.Current.CancellationToken);
    }

    private async Task CreateIntegrationTestCustomerOnStripe(CancellationToken cancellationToken)
    {
        var stripeClient = fixture.Services.GetRequiredService<StripeClient>();

        var customerEmail = "int-test@tranzrmoves.com";

        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{customerEmail}'",
        }, cancellationToken: cancellationToken);

        if (customerSearchResult.Data.FirstOrDefault() is not null)
        {
            return;
        }

        var customerOptions = new CustomerCreateOptions
        {
            Email = customerEmail,
            Name = "Tranzr Integration Test",
            Address = new AddressOptions
            {
                Line1 = "5 Holmecross Road",
                City = "Northampton",
                PostalCode = "NN3 8AW",
                Country = "GB"
            }
        };

        _ = await stripeClient.V1.Customers.CreateAsync(customerOptions, cancellationToken: cancellationToken);
        await Task.Delay(3000, cancellationToken);
    }

    public ValueTask InitializeAsync() => new(Task.CompletedTask);

    public async ValueTask DisposeAsync() => await _resetDatabase();
}
