using Microsoft.Extensions.Configuration;
using Stripe;

namespace TranzrMoves.IntegrationTests.Helpers;

/// <summary>
/// Stripe test-mode helpers for pay-later integration tests.
/// See https://docs.stripe.com/testing and https://docs.stripe.com/automated-testing
/// </summary>
internal static class PayLaterStripeTestHelper
{
    public const string E2eCustomerEmail = "paylater-e2e@tranzrmoves.com";
    public const string IntegrationTestCustomerEmail = "int-test@tranzrmoves.com";

    /// <summary>Stripe test payment method token for a successful Visa charge.</summary>
    public const string SuccessfulPaymentMethodId = "pm_card_visa";

    /// <summary>Stripe test token that declines with insufficient_funds on charge (not on attach).</summary>
    public const string InsufficientFundsPaymentMethodId = "pm_card_visa_chargeDeclinedInsufficientFunds";

    private static readonly AddressOptions GbBillingAddress = new()
    {
        Line1 = "5 Holmecross Road",
        City = "Northampton",
        PostalCode = "NN3 8AW",
        Country = "GB"
    };

    public static string RequireApiKey(IConfiguration configuration)
    {
        var key = configuration["STRIPE_API_KEY"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "STRIPE_API_KEY is required for Stripe integration tests. " +
                "Set it with: dotnet user-secrets set STRIPE_API_KEY \"sk_test_...\" " +
                "--project Tests/TranzrMoves.IntegrationTests " +
                "(or export STRIPE_API_KEY in the environment).");
        }

        if (!key.StartsWith("sk_test_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Stripe integration tests must use a Stripe test secret key (sk_test_...).");
        }

        return key;
    }

    /// <summary>
    /// Ensures a GB customer exists with a saved card via SetupIntent (mirrors pay-later checkout).
    /// Returns the payment method id to store on the Later payment row.
    /// </summary>
    public static async Task<string> EnsureCustomerWithPaymentMethodAsync(
        StripeClient stripeClient,
        CancellationToken cancellationToken)
    {
        var customer = await FindOrCreateGbCustomerAsync(stripeClient, E2eCustomerEmail, "Pay Later E2E", cancellationToken);

        var existing = await stripeClient.V1.PaymentMethods.ListAsync(new PaymentMethodListOptions
        {
            Customer = customer.Id,
            Type = "card",
            Limit = 1
        }, cancellationToken: cancellationToken);

        if (existing.Data.FirstOrDefault() is { } paymentMethod)
        {
            return paymentMethod.Id;
        }

        return await AttachTestPaymentMethodAsync(stripeClient, customer.Id, SuccessfulPaymentMethodId,
            cancellationToken);
    }

    /// <summary>
    /// Ensures a Stripe customer exists with a GB billing address (required for Stripe Tax calculations).
    /// </summary>
    public static Task<Customer> EnsureGbCustomerAsync(
        StripeClient stripeClient,
        string email,
        string name,
        CancellationToken cancellationToken) =>
        FindOrCreateGbCustomerAsync(stripeClient, email, name, cancellationToken);

    /// <summary>
    /// Attaches a Stripe test payment method token to a customer.
    /// Uses direct attach (not SetupIntent) so failure-scenario tokens (3DS, insufficient funds)
    /// can be saved without failing during setup confirmation.
    /// </summary>
    public static async Task<string> AttachTestPaymentMethodAsync(
        StripeClient stripeClient,
        string customerId,
        string testPaymentMethodToken,
        CancellationToken cancellationToken)
    {
        var paymentMethod = await stripeClient.V1.PaymentMethods.GetAsync(
            testPaymentMethodToken,
            cancellationToken: cancellationToken);

        if (paymentMethod.CustomerId == customerId)
        {
            return testPaymentMethodToken;
        }

        if (!string.IsNullOrEmpty(paymentMethod.CustomerId))
        {
            await stripeClient.V1.PaymentMethods.DetachAsync(
                testPaymentMethodToken,
                cancellationToken: cancellationToken);
        }

        await stripeClient.V1.PaymentMethods.AttachAsync(
            testPaymentMethodToken,
            new PaymentMethodAttachOptions { Customer = customerId },
            cancellationToken: cancellationToken);

        return testPaymentMethodToken;
    }

    private static async Task<Customer> FindOrCreateGbCustomerAsync(
        StripeClient stripeClient,
        string email,
        string name,
        CancellationToken cancellationToken)
    {
        var existing = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{email}'",
        }, cancellationToken: cancellationToken);

        if (existing.Data.FirstOrDefault() is { } customer)
        {
            if (HasGbAddress(customer))
            {
                return customer;
            }

            return await stripeClient.V1.Customers.UpdateAsync(customer.Id, new CustomerUpdateOptions
            {
                Address = GbBillingAddress
            }, cancellationToken: cancellationToken);
        }

        customer = await stripeClient.V1.Customers.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            Address = GbBillingAddress
        }, cancellationToken: cancellationToken);

        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        return customer;
    }

    private static bool HasGbAddress(Customer customer) =>
        !string.IsNullOrWhiteSpace(customer.Address?.Country);
}
