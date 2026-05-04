using System.Globalization;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Tax;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Infrastructure.Services;

public sealed class QuoteV2DepositBalancePaymentService(
    StripeClient stripeClient,
    ILogger<QuoteV2DepositBalancePaymentService> logger) : IQuoteV2DepositBalancePaymentService
{
    public async Task<ErrorOr<StripeIntentClientSecret>> CreateDepositBalanceAsync(
        QuoteV2 quote,
        decimal? extraCharges,
        string? extraChargesDescription,
        CancellationToken ct)
    {
        if (quote.PaymentStatus != PaymentStatus.PartiallyPaid)
        {
            return Error.Validation("QuoteV2.PaymentStatus",
                "QuoteV2 must be partially paid (deposit settled) before collecting the balance.");
        }

        var depositPayment = quote.Payments?
            .Where(p => p.PaymentType == PaymentType.Deposit && p.Status == StripePaymentStatus.Paid)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (depositPayment is null || string.IsNullOrWhiteSpace(depositPayment.PaymentMethodId))
        {
            return Error.Validation("QuoteV2.PaymentMethod",
                "Saved payment method from the deposit payment was not found.");
        }

        if (!quote.TotalCost.HasValue || quote.TotalCost.Value <= 0)
        {
            return Error.Validation("QuoteV2.TotalCost", "QuoteV2 total cost is required.");
        }

        if (string.IsNullOrWhiteSpace(quote.Customer?.Email))
        {
            return Error.Validation("QuoteV2.Customer.Email", "Customer email is required.");
        }

        var totalCost = quote.TotalCost.Value;
        var depositAmount = depositPayment.Amount ?? 0m;
        if (depositAmount <= 0)
        {
            return Error.Validation("QuoteV2.DepositAmount", "Deposit amount is missing on the quote payment record.");
        }

        var extra = extraCharges ?? 0m;
        var remainingAmount = totalCost - depositAmount;
        if (remainingAmount <= 0)
        {
            return Error.Validation("QuoteV2.Balance", "No remaining balance to charge.");
        }

        var chargeableAmount = remainingAmount + extra * 1.2m;
        var amountInCurrencyUnit = (long)Math.Round(chargeableAmount * 100, 0, MidpointRounding.AwayFromZero);

        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{quote.Customer.Email}'",
        }, cancellationToken: ct);

        var customer = customerSearchResult.Data.FirstOrDefault();
        if (customer is null)
        {
            logger.LogWarning("Stripe customer not found for QuoteV2 {QuoteId}", quote.Id);
            return Error.Validation("Stripe.Customer", "Customer not found");
        }

        var calculationOptions = new CalculationCreateOptions
        {
            Currency = CheckoutStripeConstants.Currency,
            LineItems =
            [
                new CalculationLineItemOptions
                {
                    Amount = amountInCurrencyUnit,
                    Reference = quote.QuoteReference,
                    TaxCode = CheckoutStripeConstants.TaxCode,
                    TaxBehavior = CheckoutStripeConstants.TaxBehavior
                }
            ],
            Customer = customer.Id
        };

        try
        {
            var calculation =
                await stripeClient.V1.Tax.Calculations.CreateAsync(calculationOptions, cancellationToken: ct);

            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = calculation.AmountTotal,
                Currency = CheckoutStripeConstants.Currency,
                Customer = customer.Id,
                PaymentMethod = depositPayment.PaymentMethodId,
                Description = "Tranzr Moves - Remaining balance payment",
                ReceiptEmail = customer.Email,
                Confirm = true,
                OffSession = true,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "always"
                },
                Metadata = new Dictionary<string, string>
                {
                    { nameof(PaymentMetadata.PaymentType), nameof(PaymentType.Balance) },
                    { nameof(PaymentMetadata.QuoteReference), quote.QuoteReference },
                    { nameof(PaymentMetadata.QuoteId), quote.Id.ToString() }
                }
            };

            if (extraCharges != null)
            {
                paymentIntentOptions.Metadata.Add(nameof(PaymentMetadata.ExtraCharges),
                    extra.ToString("N", CultureInfo.InvariantCulture));
                paymentIntentOptions.Metadata.Add(nameof(PaymentMetadata.ExtraChargesDescription),
                    extraChargesDescription ?? string.Empty);
            }

            var paymentIntent =
                await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions, cancellationToken: ct);

            logger.LogInformation(
                "QuoteV2 deposit balance PaymentIntent {PaymentIntentId} created for quote {QuoteId}",
                paymentIntent.Id, quote.Id);

            return new StripeIntentClientSecret
            {
                ClientSecret = paymentIntent.ClientSecret ?? string.Empty,
                IntentId = paymentIntent.Id
            };
        }
        catch (StripeException ex)
        {
            if (ex.StripeError?.Type == "card_error" && ex.StripeError.Code == "authentication_required")
            {
                logger.LogWarning("QuoteV2 deposit balance requires 3DS for quote {QuoteId}", quote.Id);
                return Error.Validation("Payment.AuthenticationRequired",
                    "Payment requires additional authentication. Please contact customer to complete payment.");
            }

            logger.LogError(ex,
                "Stripe error creating QuoteV2 deposit balance for quote {QuoteId}: {Code} - {Message}",
                quote.Id, ex.StripeError?.Code, ex.StripeError?.Message);
            return Error.Validation("Payment.Stripe", ex.StripeError?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed QuoteV2 deposit balance for quote {QuoteId}", quote.Id);
            return Error.Failure("Payment.Unknown", ex.Message);
        }
    }
}
