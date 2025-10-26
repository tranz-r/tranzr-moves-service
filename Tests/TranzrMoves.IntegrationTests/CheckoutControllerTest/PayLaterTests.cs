using System.Net;

using AutoBogus;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stripe;

using TranzrMoves.Domain.Entities;
using TranzrMoves.Infrastructure;

using Quote = TranzrMoves.Domain.Entities.Quote;

namespace TranzrMoves.IntegrationTests.CheckoutControllerTest;

public class PayLaterTests(TestServerFixture fixture) : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseStateAsync;
    private HttpClient Client => fixture.CreateClient();

    //https://docs.stripe.com/testing?testing-method=payment-methods#regulatory-cards

    [Fact]
    public async Task Successfully_Send_Checkout_Session_When_PayLater_PaymentIntent_Fails_3DS()
    {
        // Arrange
        TranzrMovesDbContext? dbContext = fixture.DbContext;

        await CreatePayLaterQuote(dbContext, "pm_card_authenticationRequired");

        var getResponse = await Client.PostAsync($"/api/v1/checkout/pay-later-collection", null);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Successfully_Send_Checkout_Session_When_Card_Has_Insufficient_Fund()
    {
        // Arrange
        TranzrMovesDbContext? dbContext = fixture.DbContext;

        await CreatePayLaterQuote(dbContext, "pm_card_visa_chargeDeclinedInsufficientFunds");

        var getResponse = await Client.PostAsync($"/api/v1/checkout/pay-later-collection", null);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Successfully_Send_Checkout_Session_When_Card_Has_Processing_Error()
    {
        // Arrange
        TranzrMovesDbContext? dbContext = fixture.DbContext;

        await CreatePayLaterQuote(dbContext, "pm_card_chargeDeclinedProcessingError");

        var getResponse = await Client.PostAsync($"/api/v1/checkout/pay-later-collection", null);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Successfully_Send_Checkout_Session_When_Card_Expires()
    {
        // Arrange
        TranzrMovesDbContext? dbContext = fixture.DbContext;

        await CreatePayLaterQuote(dbContext, "pm_card_chargeDeclinedExpiredCard");

        var getResponse = await Client.PostAsync($"/api/v1/checkout/pay-later-collection", null);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }


    private async Task CreatePayLaterQuote(TranzrMovesDbContext? dbContext, string paymentMethodId)
    {
        dbContext.ChangeTracker.Clear();

        var fakeSession = new AutoFaker<QuoteSession>()
            .Generate();

        dbContext?.Set<QuoteSession>().Add(fakeSession);
        await dbContext?.SaveChangesAsync(CancellationToken.None)!;

        var quote = new AutoFaker<Quote>()
            //.Configure(x => x.WithRecursiveDepth(1))
            .RuleFor(x => x.SessionId, f => fakeSession.SessionId)
            .RuleFor(x => x.Type, f => f.Random.Enum<QuoteType>())
            .RuleFor(x => x.PaymentType, f => PaymentType.Later)
            .RuleFor(x => x.QuoteReference, f => $"TRZ-251025-746{f.Random.Number(100, 999)}")
            .RuleFor(x => x.TotalCost, f => f.Random.Decimal(300m, 1200m))
            .RuleFor(x => x.PaymentStatus, f => PaymentStatus.PaymentSetup)
            .RuleFor(x => x.DueDate, f => DateTime.UtcNow)
            .RuleFor(x => x.CollectionDate, f => DateTime.UtcNow.AddDays(10))
            .RuleFor(x => x.DeliveryDate, f => DateTime.UtcNow.AddDays(10))
            .RuleFor(x => x.CreatedAt, () => DateTimeOffset.UtcNow)
            .RuleFor(x => x.ModifiedAt, () => DateTimeOffset.UtcNow)
            .RuleFor(x => x.ModifiedAt, () => DateTimeOffset.UtcNow)
            .RuleFor(x => x.PaymentMethodId, f => paymentMethodId)
            .Generate();

        dbContext?.Set<Quote>().Add(quote);
        await dbContext?.SaveChangesAsync(CancellationToken.None)!;

        var user = new AutoFaker<User>()
            .RuleFor(x => x.Email, f => "int-test@tranzrmoves.com")
            .RuleFor(x => x.FullName, f => $"{f.Name.FirstName()}  {f.Name.LastName()}")
            .Generate();

        dbContext?.Set<User>().Add(user);
        await dbContext?.SaveChangesAsync(CancellationToken.None)!;

        var userQuote = new AutoFaker<CustomerQuote>()
            .RuleFor(x => x.UserId, f => user.Id)
            .RuleFor(x => x.QuoteId, f => quote.Id)
            .Generate();

        dbContext?.Set<CustomerQuote>().Add(userQuote);
        await dbContext?.SaveChangesAsync(CancellationToken.None)!;

        await CreateIntegrationTestCustomerOnStripe(CancellationToken.None);
    }

    private async Task CreateIntegrationTestCustomerOnStripe(CancellationToken cancellationToken)
    {
        var stripeClient = fixture.Services.GetRequiredService<StripeClient>();

        var addressOptionFaker = new AutoFaker<AddressOptions>()
            // .Configure(x => x.WithRecursiveDepth(1))
            .RuleFor(x => x.Line1, f => "5 Holmecross Road")
            .RuleFor(x => x.City, f => "Northampton")
            .RuleFor(x => x.PostalCode, f => "NN3 8AW")
            .RuleFor(x => x.Country, f => "GB")
            .Generate();

        var customerEmail = "int-test@tranzrmoves.com";

        // Create Stripe customer if not exists
        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{customerEmail}'",
        }, cancellationToken: cancellationToken);

        var stripeCustomer = customerSearchResult.Data.FirstOrDefault();

        if (stripeCustomer == null)
        {
            var customerOptions = new CustomerCreateOptions
            {
                Email = customerEmail,
                Name = "Tranzr Integration Test",
                Address = addressOptionFaker
            };

            _ = await stripeClient.V1.Customers.CreateAsync(customerOptions, cancellationToken: cancellationToken);

            await Task.Delay(3000, cancellationToken);
        }
    }

    private async Task<PaymentMethod> CreatePaymentMethod(string cardNumber)
    {
        // Create a payment method with the test card
        var paymentMethodOptions = new PaymentMethodCreateOptions
        {
            Type = "card",
            Card = new PaymentMethodCardOptions
            {
                Number = cardNumber,
                ExpMonth = 12,
                ExpYear = DateTime.Now.Year + 1,
                Cvc = "123",
            },
        };


        var stripeClient = fixture.Services.GetRequiredService<StripeClient>();

        var paymentMethod = await stripeClient.V1.PaymentMethods.CreateAsync(paymentMethodOptions);

        return paymentMethod;

        // // Create and confirm a payment intent with this payment method
        // var paymentIntentOptions = new PaymentIntentCreateOptions
        // {
        //     Amount = 2000,
        //     Currency = "usd",
        //     PaymentMethod = paymentMethod.Id,
        //     Confirm = true,
        //     ReturnUrl = "https://example.com/return", // For 3DS scenarios
        // };
        //
        //
        // await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions);
        //
        // Console.WriteLine($"Scenario {scenario} succeeded unexpectedly");
    }


    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
