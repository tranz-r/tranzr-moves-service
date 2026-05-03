using System.Globalization;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Tax;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Infrastructure.Services;

public sealed class QuoteV2PaymentSheetService(
    StripeClient stripeClient,
    IQuoteRepository quoteRepository,
    ITimeService timeService,
    ILogger<QuoteV2PaymentSheetService> logger) : IQuoteV2PaymentSheetService
{
    public async Task<ErrorOr<QuoteV2PaymentSheetResult>> CreateAsync(
        QuoteV2 quote,
        PaymentType paymentType,
        CancellationToken ct)
    {
        if (quote.Customer?.Email is null)
        {
            return Error.Validation("QuoteV2.Customer.Email", "QuoteV2 customer email is required.");
        }

        if (quote.TotalCost is null or <= 0)
        {
            return Error.Validation("QuoteV2.TotalCost",
                "QuoteV2 total cost must be calculated before creating payment sheet.");
        }

        if (quote.Schedule?.CollectionDate is null)
        {
            return Error.Validation("QuoteV2.Schedule.CollectionDate",
                "QuoteV2 collection date is required before creating payment sheet.");
        }

        if (paymentType is PaymentType.Balance or PaymentType.Adhoc)
        {
            return Error.Validation("PaymentType",
                "QuoteV2 payment sheet supports only Full, Deposit, or Later payment types.");
        }

        var selectedPayment = UpsertRequestedPaymentSelection(quote, paymentType);
        var customer = await UpsertStripeCustomerForQuoteV2Async(quote, ct);
        var ephemeralKey = await stripeClient.V1.EphemeralKeys.CreateAsync(new EphemeralKeyCreateOptions
        {
            Customer = customer.Id,
            StripeVersion = "2026-03-25.dahlia"
        }, cancellationToken: ct);

        var paymentAmount = ResolvePaymentAmountPence(quote, selectedPayment);
        var description = ResolvePaymentDescription(selectedPayment.PaymentType);

        if (selectedPayment.PaymentType == PaymentType.Later)
        {
            if (!string.IsNullOrWhiteSpace(selectedPayment.SetupIntentId))
            {
                var existingSetupIntent = await TryGetSetupIntentAsync(selectedPayment.SetupIntentId, ct);
                if (existingSetupIntent is not null && IsReusableSetupIntentStatus(existingSetupIntent.Status))
                {
                    var reuseSaveResult = await quoteRepository.SaveChangesAsync(ct);
                    if (reuseSaveResult.IsError)
                    {
                        return reuseSaveResult.Errors;
                    }

                    return new QuoteV2PaymentSheetResult
                    {
                        ClientSecret = existingSetupIntent.ClientSecret ?? string.Empty,
                        IntentId = existingSetupIntent.Id,
                        EphemeralKey = ephemeralKey.Secret ?? string.Empty,
                        CustomerId = customer.Id
                    };
                }
            }

            var moveCalendarDay = quote.Schedule.CollectionDate.Value.InUtc().Date;
            var paymentDueDate = moveCalendarDay.PlusDays(-3);
            var setupIntent = await stripeClient.V1.SetupIntents.CreateAsync(new SetupIntentCreateOptions
            {
                Customer = customer.Id,
                Description = description,
                Usage = "off_session",
                PaymentMethodTypes = ["card", "link"],
                Metadata = new Dictionary<string, string>
                {
                    { nameof(PaymentMetadata.PaymentType), selectedPayment.PaymentType.ToString() },
                    { nameof(PaymentMetadata.TotalCost), quote.TotalCost.Value.ToString("F2", CultureInfo.InvariantCulture) },
                    { nameof(PaymentMetadata.DepositPercentage), "0" },
                    { nameof(PaymentMetadata.DueDate), paymentDueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                    { nameof(PaymentMetadata.QuoteReference), quote.QuoteReference },
                    { nameof(PaymentMetadata.QuoteId), quote.Id.ToString() },
                    {
                        nameof(PaymentMetadata.PaymentDueDate),
                        selectedPayment.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty
                    }
                }
            }, cancellationToken: ct);

            selectedPayment.SetupIntentId = setupIntent.Id;
            selectedPayment.RemainingAmount = quote.TotalCost.Value;
            selectedPayment.DueDate = paymentDueDate;
            selectedPayment.Status = StripePaymentStatus.Pending;

            var saveResult = await quoteRepository.SaveChangesAsync(ct);
            if (saveResult.IsError)
            {
                return saveResult.Errors;
            }

            return new QuoteV2PaymentSheetResult
            {
                ClientSecret = setupIntent.ClientSecret ?? string.Empty,
                IntentId = setupIntent.Id,
                EphemeralKey = ephemeralKey.Secret ?? string.Empty,
                CustomerId = customer.Id
            };
        }

        if (!string.IsNullOrWhiteSpace(selectedPayment.PaymentIntentId))
        {
            var existingPaymentIntent = await TryGetPaymentIntentAsync(selectedPayment.PaymentIntentId, ct);
            if (existingPaymentIntent is not null && IsReusablePaymentIntentStatus(existingPaymentIntent.Status))
            {
                selectedPayment.Amount = existingPaymentIntent.Amount / 100m;
                selectedPayment.Status = StripePaymentStatus.Pending;

                var reuseSaveResult = await quoteRepository.SaveChangesAsync(ct);
                if (reuseSaveResult.IsError)
                {
                    return reuseSaveResult.Errors;
                }

                return new QuoteV2PaymentSheetResult
                {
                    ClientSecret = existingPaymentIntent.ClientSecret ?? string.Empty,
                    IntentId = existingPaymentIntent.Id,
                    EphemeralKey = ephemeralKey.Secret ?? string.Empty,
                    CustomerId = customer.Id
                };
            }
        }

        var calculation = await stripeClient.V1.Tax.Calculations.CreateAsync(new CalculationCreateOptions
        {
            Currency = CheckoutStripeConstants.Currency,
            LineItems =
            [
                new CalculationLineItemOptions
                {
                    Amount = paymentAmount,
                    Reference = quote.QuoteReference,
                    TaxCode = CheckoutStripeConstants.TaxCode,
                    TaxBehavior = CheckoutStripeConstants.TaxBehavior
                }
            ],
            Customer = customer.Id
        }, cancellationToken: ct);

        var paymentIntentOptions = new PaymentIntentCreateOptions
        {
            Amount = calculation.AmountTotal,
            Currency = CheckoutStripeConstants.Currency,
            Customer = customer.Id,
            Description = description,
            ReceiptEmail = quote.Customer.Email,
            Metadata = new Dictionary<string, string>
            {
                { nameof(PaymentMetadata.PaymentType), selectedPayment.PaymentType.ToString() },
                { nameof(PaymentMetadata.TotalCost), quote.TotalCost.Value.ToString("F2", CultureInfo.InvariantCulture) },
                { nameof(PaymentMetadata.DepositPercentage), "25" },
                {
                    nameof(PaymentMetadata.DueDate),
                    selectedPayment.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty
                },
                { nameof(PaymentMetadata.QuoteReference), quote.QuoteReference },
                { nameof(PaymentMetadata.QuoteId), quote.Id.ToString() }
            }
        };

        if (selectedPayment.PaymentType == PaymentType.Full)
        {
            paymentIntentOptions.AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "always"
            };

            selectedPayment.RemainingAmount = 0m;
        }
        else
        {
            paymentIntentOptions.PaymentMethodTypes = ["card", "link"];
            paymentIntentOptions.SetupFutureUsage = "off_session";
            selectedPayment.RemainingAmount = quote.TotalCost.Value - quote.TotalCost * 0.25m;
            selectedPayment.DueDate = quote.Schedule.CollectionDate.Value.InUtc().Date;
        }

        var paymentIntent = await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions, cancellationToken: ct);
        selectedPayment.PaymentIntentId = paymentIntent.Id;
        selectedPayment.Amount = calculation.AmountTotal / 100m;
        selectedPayment.Status = StripePaymentStatus.Pending;

        var paymentSaveResult = await quoteRepository.SaveChangesAsync(ct);
        if (paymentSaveResult.IsError)
        {
            return paymentSaveResult.Errors;
        }

        return new QuoteV2PaymentSheetResult
        {
            ClientSecret = paymentIntent.ClientSecret ?? string.Empty,
            IntentId = paymentIntent.Id,
            EphemeralKey = ephemeralKey.Secret ?? string.Empty,
            CustomerId = customer.Id
        };
    }

    private Payment UpsertRequestedPaymentSelection(QuoteV2 quote, PaymentType paymentType)
    {
        quote.Payments ??= [];
        var payments = quote.Payments;
        var now = timeService.Now();

        foreach (var payment in
                 payments.Where(p => p.PaymentType is not PaymentType.Balance and not PaymentType.Adhoc
                 && p.CustomerSelectedOption))
        {
            payment.CustomerSelectedOption = false;
            payment.ModifiedAt = now;
            payment.ModifiedBy = nameof(QuoteV2PaymentSheetService);
        }

        var selectedPayment = payments
                                  .Where(p => p.PaymentType == paymentType)
                                  .OrderByDescending(p => p.CreatedAt)
                                  .FirstOrDefault();

        if (selectedPayment is null)
        {
            selectedPayment = new Payment
            {
                QuoteId = quote.Id,
                PaymentType = paymentType,
                Status = StripePaymentStatus.Pending,
                CustomerSelectedOption = true,
                CreatedAt = now,
                CreatedBy = nameof(QuoteV2PaymentSheetService),
                ModifiedAt = now,
                ModifiedBy = nameof(QuoteV2PaymentSheetService)
            };

            payments.Add(selectedPayment);
            return selectedPayment;
        }

        selectedPayment.CustomerSelectedOption = true;
        selectedPayment.ModifiedAt = now;
        selectedPayment.ModifiedBy = nameof(QuoteV2PaymentSheetService);

        return selectedPayment;
    }

    private async Task<PaymentIntent?> TryGetPaymentIntentAsync(string paymentIntentId, CancellationToken ct)
    {
        try
        {
            return await stripeClient.V1.PaymentIntents.GetAsync(paymentIntentId, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Unable to reuse PaymentIntent {PaymentIntentId}. A new intent will be created.",
                paymentIntentId);
            return null;
        }
    }

    private async Task<SetupIntent?> TryGetSetupIntentAsync(string setupIntentId, CancellationToken ct)
    {
        try
        {
            return await stripeClient.V1.SetupIntents.GetAsync(setupIntentId, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Unable to reuse SetupIntent {SetupIntentId}. A new intent will be created.",
                setupIntentId);
            return null;
        }
    }

    private static bool IsReusablePaymentIntentStatus(string? status) =>
        status is not null && status.Equals("requires_payment_method", StringComparison.OrdinalIgnoreCase)
            || status is not null && status.Equals("requires_confirmation", StringComparison.OrdinalIgnoreCase)
            || status is not null && status.Equals("requires_action", StringComparison.OrdinalIgnoreCase)
            || status is not null && status.Equals("requires_capture", StringComparison.OrdinalIgnoreCase)
            || status is not null && status.Equals("processing", StringComparison.OrdinalIgnoreCase);

    private static bool IsReusableSetupIntentStatus(string? status) =>
        status is not null && status.Equals("requires_payment_method", StringComparison.OrdinalIgnoreCase)
            || status is not null && status.Equals("requires_confirmation", StringComparison.OrdinalIgnoreCase)
            || status is not null && status.Equals("requires_action", StringComparison.OrdinalIgnoreCase)
            || status is not null && status.Equals("processing", StringComparison.OrdinalIgnoreCase);

    private async Task<Stripe.Customer> UpsertStripeCustomerForQuoteV2Async(QuoteV2 quote, CancellationToken ct)
    {
        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{quote.Customer!.Email}'",
        }, cancellationToken: ct);

        var customer = customerSearchResult.Data.FirstOrDefault();
        var fullName = $"{quote.Customer.FirstName?.Trim()} {quote.Customer.LastName?.Trim()}";

        if (customer is null)
        {
            return await stripeClient.V1.Customers.CreateAsync(new CustomerCreateOptions
            {
                Email = quote.Customer.Email,
                Name = fullName,
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

        return await stripeClient.V1.Customers.UpdateAsync(customer.Id, new CustomerUpdateOptions
        {
            Name = fullName,
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

    private static long ResolvePaymentAmountPence(QuoteV2 quote, Payment selectedPayment)
    {
        var total = quote.TotalCost ?? 0m;
        var amount = selectedPayment.PaymentType switch
        {
            PaymentType.Full => total,
            PaymentType.Deposit => total * 0.25m,
            _ => 0m
        };

        return (long)Math.Round(amount * 100, 0, MidpointRounding.AwayFromZero);
    }

    private static string ResolvePaymentDescription(PaymentType paymentType) => paymentType switch
    {
        PaymentType.Full => "Your Tranzr Moves payment - Full amount",
        PaymentType.Deposit => "Your Tranzr Moves payment - 25% deposit",
        PaymentType.Later => "Your Tranzr Moves payment - Payment deferred",
        _ => "Your Tranzr Moves payment"
    };
}
