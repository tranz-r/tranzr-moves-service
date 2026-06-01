using System.Globalization;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Tax;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public sealed class QuoteV2DepositBalanceCollectionService(
    StripeClient stripeClient,
    IQuoteRepository quoteRepository,
    IQuoteV2HostedCheckoutSessionService hostedCheckoutSessionService,
    ITimeService timeService,
    ILogger<QuoteV2DepositBalanceCollectionService> logger) : IQuoteV2DepositBalanceCollectionService
{
    private static readonly HashSet<string> TerminalPaymentIntentStatuses =
    [
        "succeeded",
        "processing",
        "requires_capture"
    ];

    public async Task<ErrorOr<Success>> TryCollectAsync(Guid quoteId, CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetQuoteByIdAsync(quoteId, cancellationToken, asTracking: true);
        if (quote is null)
        {
            logger.LogWarning("QuoteV2 {QuoteId} not found for deposit balance collection", quoteId);
            return Result.Success;
        }

        if (quote.PaymentStatus != PaymentStatus.PartiallyPaid)
        {
            logger.LogInformation(
                "Skipping deposit balance collection for {QuoteRef}; status is {Status}",
                quote.QuoteReference,
                quote.PaymentStatus);
            return Result.Success;
        }

        var balancePayment = quote.Payments?
            .Where(p => p.PaymentType == PaymentType.Balance)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (balancePayment?.Status == StripePaymentStatus.Paid)
        {
            logger.LogInformation("Skipping deposit balance collection for {QuoteRef}; balance already paid",
                quote.QuoteReference);
            return Result.Success;
        }

        if (!string.IsNullOrEmpty(balancePayment?.PaymentIntentId))
        {
            try
            {
                var existingIntent = await stripeClient.V1.PaymentIntents.GetAsync(balancePayment.PaymentIntentId,
                    cancellationToken: cancellationToken);
                if (TerminalPaymentIntentStatuses.Contains(existingIntent.Status))
                {
                    logger.LogInformation(
                        "Skipping deposit balance collection for {QuoteRef}; PI {PaymentIntentId} is {Status}",
                        quote.QuoteReference,
                        existingIntent.Id,
                        existingIntent.Status);
                    return Result.Success;
                }
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex,
                    "Could not load existing PI {PaymentIntentId} for quote {QuoteRef}; proceeding with collection",
                    balancePayment.PaymentIntentId,
                    quote.QuoteReference);
            }
        }

        return await CollectAsync(quote, cancellationToken: cancellationToken);
    }

    public Task<ErrorOr<Success>> CollectAsync(
        QuoteV2 quote,
        decimal? extraCharges = null,
        string? extraChargesDescription = null,
        CancellationToken cancellationToken = default) =>
        CollectInternalAsync(quote, extraCharges, extraChargesDescription, cancellationToken);

    private async Task<ErrorOr<Success>> CollectInternalAsync(
        QuoteV2 quote,
        decimal? extraCharges,
        string? extraChargesDescription,
        CancellationToken cancellationToken)
    {
        var depositPayment = quote.Payments?
            .Where(p => p.PaymentType == PaymentType.Deposit && p.Status == StripePaymentStatus.Paid)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (depositPayment?.PaymentMethodId is null)
        {
            logger.LogWarning("No paid Deposit payment with PaymentMethodId for quote {QuoteRef}", quote.QuoteReference);
            return Error.Failure("DepositBalance.NoPaymentMethod", "Saved payment method from deposit was not found.");
        }

        if (quote.Customer?.Email is null)
        {
            return Error.Failure("DepositBalance.NoEmail", "Customer email is required.");
        }

        if (quote.TotalCost is null or <= 0)
        {
            return Error.Failure("DepositBalance.NoTotal", "Quote total is required.");
        }

        var depositAmount = depositPayment.Amount ?? 0m;
        if (depositAmount <= 0)
        {
            return Error.Failure("DepositBalance.NoDepositAmount", "Deposit amount is missing on the quote payment record.");
        }

        var extra = extraCharges ?? 0m;
        var remainingAmount = quote.TotalCost.Value - depositAmount;
        if (remainingAmount <= 0)
        {
            return Error.Failure("DepositBalance.NoBalance", "No remaining balance to charge.");
        }

        var chargeableAmount = remainingAmount + extra * 1.2m;

        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{quote.Customer.Email}'",
        }, cancellationToken: cancellationToken);

        var customer = customerSearchResult.Data.FirstOrDefault();
        if (customer is null)
        {
            logger.LogWarning("Stripe customer not found for quote {QuoteRef}", quote.QuoteReference);
            return Error.Failure("DepositBalance.NoStripeCustomer", "Stripe customer not found.");
        }

        var amountInCurrencyUnit =
            (long)Math.Round(chargeableAmount * 100, 0, MidpointRounding.AwayFromZero);

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

        var calculation =
            await stripeClient.V1.Tax.Calculations.CreateAsync(calculationOptions,
                cancellationToken: cancellationToken);

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

        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"quote-v2-deposit-balance-{quote.Id:N}"
        };

        try
        {
            var paymentIntent = await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions,
                requestOptions,
                cancellationToken);

            quote.Payments ??= [];
            var now = timeService.Now();
            var balancePayment = quote.Payments.FirstOrDefault(p => p.PaymentType == PaymentType.Balance);
            if (balancePayment is null)
            {
                balancePayment = new Payment
                {
                    QuoteId = quote.Id,
                    PaymentType = PaymentType.Balance,
                    Status = StripePaymentStatus.Pending,
                    CreatedAt = now,
                    CreatedBy = nameof(QuoteV2DepositBalanceCollectionService),
                    ModifiedAt = now,
                    ModifiedBy = nameof(QuoteV2DepositBalanceCollectionService)
                };
                quote.Payments.Add(balancePayment);
                quoteRepository.AddPayment(balancePayment);
            }

            balancePayment.PaymentIntentId = paymentIntent.Id;
            balancePayment.ModifiedAt = now;
            balancePayment.ModifiedBy = nameof(QuoteV2DepositBalanceCollectionService);

            var save = await quoteRepository.SaveChangesAsync(cancellationToken);
            if (save.IsError)
            {
                logger.LogWarning("Failed to persist deposit balance PI for {QuoteRef}", quote.QuoteReference);
                return save.Errors;
            }

            logger.LogInformation("QuoteV2 deposit balance intent created for {QuoteRef}", quote.QuoteReference);
            return Result.Success;
        }
        catch (StripeException ex)
        {
            if (ex.StripeError.Type == "card_error")
            {
                var recovery = await TrySendRecoveryCheckoutAsync(quote, depositPayment.PaymentMethodId,
                    chargeableAmount, ex, cancellationToken);
                if (recovery.IsError)
                {
                    return recovery.Errors;
                }

                return Result.Success;
            }

            logger.LogError(ex, "Stripe error collecting deposit balance for {QuoteRef}", quote.QuoteReference);
            return Error.Failure("DepositBalance.Stripe", ex.StripeError.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed deposit balance collection for {QuoteRef}", quote.QuoteReference);
            return Error.Failure("DepositBalance.Unknown", ex.Message);
        }
    }

    private async Task<ErrorOr<Success>> TrySendRecoveryCheckoutAsync(
        QuoteV2 quote,
        string paymentMethodId,
        decimal checkoutAmount,
        StripeException ex,
        CancellationToken cancellationToken)
    {
        try
        {
            var pm = await stripeClient.V1.PaymentMethods.GetAsync(paymentMethodId,
                cancellationToken: cancellationToken);
            var cardLast4 = pm.Card?.Last4 ?? "????";
            var cardBrand = pm.Card?.Brand ?? "card";
            var cardErrorReason = BalanceChargeRecoveryHelper.MapCardErrorReason(ex);
            var cardErrorDescription = BalanceChargeRecoveryHelper.BuildCardErrorDescription(
                quote, cardBrand, cardLast4, cardErrorReason);

            var checkout = await hostedCheckoutSessionService.CreateAsync(
                quote,
                checkoutAmount,
                $"Remaining balance for quote #{quote.QuoteReference}",
                "checkout-session-payment-error",
                PaymentType.Full,
                cardErrorDescription,
                [ToBccEmails.PayLaterWarning],
                cancellationToken);

            if (checkout.IsError)
            {
                return checkout.Errors;
            }

            return Result.Success;
        }
        catch (Exception inner)
        {
            logger.LogError(inner, "Recovery checkout failed for {QuoteRef}", quote.QuoteReference);
            return Error.Failure("DepositBalance.Recovery", inner.Message);
        }
    }
}
