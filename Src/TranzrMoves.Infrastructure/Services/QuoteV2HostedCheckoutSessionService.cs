using System.Globalization;
using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Stripe;
using Stripe.Checkout;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Infrastructure.Services;

public sealed class QuoteV2HostedCheckoutSessionService(
    StripeClient stripeClient,
    IConfiguration configuration,
    IQuoteRepository quoteRepository,
    IEmailService emailService,
    ITemplateService templateService,
    ITimeService timeService,
    ILogger<QuoteV2HostedCheckoutSessionService> logger) : IQuoteV2HostedCheckoutSessionService
{
    public async Task<ErrorOr<QuoteV2HostedCheckoutSessionResult>> CreateAsync(
        QuoteV2 quote,
        decimal amount,
        string description,
        string emailTemplateBaseName,
        PaymentType paymentMetadataType,
        string? cardErrorReason,
        IReadOnlyList<string>? bccRecipients,
        CancellationToken cancellationToken)
    {
        if (quote.Customer?.Email is null or "")
        {
            return Error.Validation("QuoteV2.Customer.Email", "Customer email is required.");
        }

        if (amount <= 0)
        {
            return Error.Validation("Amount", "Amount must be positive.");
        }

        var stripeCustomer = await UpsertStripeCustomerAsync(quote, cancellationToken);

        var priceId = await CreateOrGetPriceAsync(amount,
            string.IsNullOrWhiteSpace(description)
                ? $"Payment for quote #{quote.QuoteReference}"
                : description,
            quote.QuoteReference,
            cancellationToken);

        var selectedPayment = UpsertPaymentSelectionForHostedCheckout(quote, paymentMetadataType);
        var now = timeService.Now();
        selectedPayment.ModifiedAt = now;
        selectedPayment.ModifiedBy = nameof(QuoteV2HostedCheckoutSessionService);

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            Customer = stripeCustomer.Id,
            ClientReferenceId = quote.QuoteReference,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            ],
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
            Metadata = new Dictionary<string, string>
            {
                { nameof(PaymentMetadata.QuoteReference), quote.QuoteReference },
                { nameof(PaymentMetadata.QuoteId), quote.Id.ToString() },
                { nameof(PaymentMetadata.CustomerEmail), quote.Customer.Email },
                { nameof(PaymentMetadata.PaymentAmount), amount.ToString("F2", CultureInfo.InvariantCulture) },
                { nameof(PaymentMetadata.ExtraChargesDescription), description },
                { nameof(PaymentMetadata.PaymentType), paymentMetadataType.ToString() }
            },
            SuccessUrl = configuration["CHECKOUT_SESSION_SUCCESS_URL"],
            CancelUrl = configuration["CHECKOUT_SESSION_CANCEL_URL"]
        };

        var sessionService = new SessionService(stripeClient);
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        selectedPayment.StripeSessionId = session.Id;

        var saveResult = await quoteRepository.SaveChangesAsync(cancellationToken);
        if (saveResult.IsError)
        {
            return saveResult.Errors;
        }

        var emailSent = false;
        try
        {
            var customerName = $"{quote.Customer.FirstName?.Trim()} {quote.Customer.LastName?.Trim()}".Trim();
            if (string.IsNullOrEmpty(customerName))
            {
                customerName = quote.Customer.Email;
            }

            var templateData = new
            {
                customerName,
                paymentAmount = amount.ToString("N2", CultureInfo.InvariantCulture),
                quoteReference = quote.QuoteReference,
                checkoutUrl = session.Url,
                description = string.IsNullOrWhiteSpace(description)
                    ? $"Payment for quote #{quote.QuoteReference}"
                    : description,
                currentYear = timeService.NowInUtc().Year,
                cardErrorReason
            };

            var subject = $"Checkout Link - #{quote.QuoteReference}";
            var htmlEmail = templateService.GenerateEmail($"{emailTemplateBaseName}.html", templateData);
            var textEmail = templateService.GenerateEmail($"{emailTemplateBaseName}.txt", templateData);
            await emailService.SendBookingConfirmationEmailAsync(FromEmails.Booking, subject, quote.Customer.Email,
                htmlEmail, textEmail, bccRecipients?.ToList());
            emailSent = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send checkout session email to {Email}", quote.Customer.Email);
        }

        return new QuoteV2HostedCheckoutSessionResult(
            session.Id,
            session.Url ?? string.Empty,
            quote.QuoteReference,
            amount,
            emailSent);
    }

    private Payment UpsertPaymentSelectionForHostedCheckout(QuoteV2 quote, PaymentType paymentType)
    {
        quote.Payments ??= [];
        var payments = quote.Payments;
        var now = timeService.Now();

        foreach (var payment in payments.Where(p =>
                     p.PaymentType is not PaymentType.Balance and not PaymentType.Adhoc))
        {
            payment.CustomerSelectedOption = false;
        }

        var selectedPayment = payments
                                  .Where(p => p.PaymentType == paymentType)
                                  .OrderByDescending(p => p.CreatedAt)
                                  .FirstOrDefault()
                              ?? new Payment
                              {
                                  Id = Guid.NewGuid(),
                                  QuoteId = quote.Id,
                                  PaymentType = paymentType,
                                  Status = StripePaymentStatus.Pending,
                                  CreatedAt = now,
                                  CreatedBy = nameof(QuoteV2HostedCheckoutSessionService),
                                  ModifiedAt = now,
                                  ModifiedBy = nameof(QuoteV2HostedCheckoutSessionService)
                              };

        selectedPayment.CustomerSelectedOption = true;

        if (!payments.Any(p => p.Id == selectedPayment.Id))
        {
            payments.Add(selectedPayment);
        }

        return selectedPayment;
    }

    private async Task<Customer> UpsertStripeCustomerAsync(QuoteV2 quote, CancellationToken ct)
    {
        var email = quote.Customer!.Email!;
        var sr = await stripeClient.V1.Customers.SearchAsync(
            new CustomerSearchOptions { Query = $"email:'{email}'" }, cancellationToken: ct);

        var existing = sr.Data.FirstOrDefault();
        var fullName = $"{quote.Customer.FirstName?.Trim()} {quote.Customer.LastName?.Trim()}".Trim();

        if (existing is null)
        {
            return await stripeClient.V1.Customers.CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Name = string.IsNullOrEmpty(fullName) ? email : fullName,
                Address = quote.Customer.BillingAddress is null
                    ? null
                    : new AddressOptions
                    {
                        Line1 = quote.Customer.BillingAddress.Line1,
                        Line2 = quote.Customer.BillingAddress.Line2,
                        City = quote.Customer.BillingAddress.City,
                        PostalCode = quote.Customer.BillingAddress.PostCode,
                        Country = quote.Customer.BillingAddress.Country ?? "United Kingdom"
                    }
            }, cancellationToken: ct);
        }

        return await stripeClient.V1.Customers.UpdateAsync(existing.Id, new CustomerUpdateOptions
        {
            Name = string.IsNullOrEmpty(fullName) ? existing.Name : fullName,
            Address = quote.Customer.BillingAddress is null
                ? null
                : new AddressOptions
                {
                    Line1 = quote.Customer.BillingAddress.Line1,
                    Line2 = quote.Customer.BillingAddress.Line2,
                    City = quote.Customer.BillingAddress.City,
                    PostalCode = quote.Customer.BillingAddress.PostCode,
                    Country = quote.Customer.BillingAddress.Country
                }
        }, cancellationToken: ct);
    }

    private async Task<string> CreateOrGetPriceAsync(
        decimal amount,
        string description,
        string quoteReference,
        CancellationToken cancellationToken)
    {
        var productName = $"Tranzr Moves - Quote #{quoteReference}";

        var productSearchOptions = new ProductSearchOptions
        {
            Query = $"name:'{productName}'",
            Limit = 1
        };

        var existingProducts = await stripeClient.V1.Products.SearchAsync(productSearchOptions,
            cancellationToken: cancellationToken);

        Product product;
        if (existingProducts.Data.Any())
        {
            product = existingProducts.Data.First();
        }
        else
        {
            product = await stripeClient.V1.Products.CreateAsync(new ProductCreateOptions
            {
                Name = productName,
                Description = description,
                Type = "service"
            }, cancellationToken: cancellationToken);
        }

        var price = await stripeClient.V1.Prices.CreateAsync(new PriceCreateOptions
        {
            Product = product.Id,
            Currency = CheckoutStripeConstants.Currency,
            UnitAmount = (long)Math.Round(amount * 100, 0, MidpointRounding.AwayFromZero),
            TaxBehavior = CheckoutStripeConstants.TaxBehavior
        }, cancellationToken: cancellationToken);

        return price.Id;
    }
}
