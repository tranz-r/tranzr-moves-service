using ErrorOr;
using Microsoft.Extensions.Logging;
using NodaTime.Text;
using Stripe;
using Stripe.Tax;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public sealed class QuoteV2LaterBalanceCollectionService(
    StripeClient stripeClient,
    IQuoteRepository quoteRepository,
    IQuoteV2HostedCheckoutSessionService hostedCheckoutSessionService,
    ITimeService timeService,
    ILogger<QuoteV2LaterBalanceCollectionService> logger) : IQuoteV2LaterBalanceCollectionService
{
    private static readonly LocalDatePattern IsoDatePattern = LocalDatePattern.Iso;

    public async Task<ErrorOr<Success>> CollectAsync(QuoteV2 quote, CancellationToken cancellationToken)
    {
        var laterPayment = quote.Payments?
            .Where(p => p.PaymentType == PaymentType.Later)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (laterPayment?.PaymentMethodId is null)
        {
            logger.LogWarning("No Later payment with PaymentMethodId for quote {QuoteRef}", quote.QuoteReference);
            return Error.Failure("PayLater.NoPaymentMethod", "Saved payment method not found.");
        }

        if (quote.Customer?.Email is null)
        {
            return Error.Failure("PayLater.NoEmail", "Customer email is required.");
        }

        if (quote.TotalCost is null or <= 0)
        {
            return Error.Failure("PayLater.NoTotal", "Quote total is required.");
        }

        var customerSearchResult = await stripeClient.V1.Customers.SearchAsync(new CustomerSearchOptions
        {
            Query = $"email:'{quote.Customer.Email}'",
        }, cancellationToken: cancellationToken);

        var customer = customerSearchResult.Data.FirstOrDefault();
        if (customer is null)
        {
            logger.LogWarning("Stripe customer not found for quote {QuoteRef}", quote.QuoteReference);
            return Error.Failure("PayLater.NoStripeCustomer", "Stripe customer not found.");
        }

        var amountInCurrencyUnit =
            (long)Math.Round(quote.TotalCost.Value * 100, 0, MidpointRounding.AwayFromZero);

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
            PaymentMethod = laterPayment.PaymentMethodId,
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

        try
        {
            var paymentIntent = await stripeClient.V1.PaymentIntents.CreateAsync(paymentIntentOptions,
                cancellationToken: cancellationToken);

            quote.Payments ??= [];
            var now = timeService.Now();
            var balancePayment = quote.Payments.FirstOrDefault(p => p.PaymentType == PaymentType.Balance)
                                 ?? new Payment
                                 {
                                     Id = Guid.NewGuid(),
                                     QuoteId = quote.Id,
                                     PaymentType = PaymentType.Balance,
                                     Status = StripePaymentStatus.Pending,
                                     CreatedAt = now,
                                     CreatedBy = nameof(QuoteV2LaterBalanceCollectionService),
                                     ModifiedAt = now,
                                     ModifiedBy = nameof(QuoteV2LaterBalanceCollectionService)
                                 };

            balancePayment.PaymentIntentId = paymentIntent.Id;
            balancePayment.ModifiedAt = now;
            balancePayment.ModifiedBy = nameof(QuoteV2LaterBalanceCollectionService);

            if (!quote.Payments.Any(p => p.Id == balancePayment.Id))
            {
                quote.Payments.Add(balancePayment);
            }

            var save = await quoteRepository.SaveChangesAsync(cancellationToken);
            if (save.IsError)
            {
                logger.LogWarning("Failed to persist pay-later PI for {QuoteRef}", quote.QuoteReference);
            }

            logger.LogInformation("QuoteV2 pay-later intent created for {QuoteRef}", quote.QuoteReference);
            return Result.Success;
        }
        catch (StripeException ex)
        {
            if (ex.StripeError.Type == "card_error")
            {
                var recovery = await TrySendRecoveryCheckoutAsync(quote, laterPayment.PaymentMethodId, ex,
                    cancellationToken);
                if (recovery.IsError)
                {
                    return recovery.Errors;
                }

                return Result.Success;
            }

            logger.LogError(ex, "Stripe error collecting pay-later for {QuoteRef}", quote.QuoteReference);
            return Error.Failure("PayLater.Stripe", ex.StripeError.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed pay-later collection for {QuoteRef}", quote.QuoteReference);
            return Error.Failure("PayLater.Unknown", ex.Message);
        }
    }

    private async Task<ErrorOr<Success>> TrySendRecoveryCheckoutAsync(
        QuoteV2 quote,
        string paymentMethodId,
        StripeException ex,
        CancellationToken cancellationToken)
    {
        try
        {
            var pm = await stripeClient.V1.PaymentMethods.GetAsync(paymentMethodId,
                cancellationToken: cancellationToken);
            var cardLast4 = pm.Card?.Last4 ?? "????";
            var cardBrand = pm.Card?.Brand ?? "card";
            var cardErrorReason = MapCardErrorReason(ex);
            var collectionDate = quote.Schedule?.CollectionDate?.InUtc().Date;
            var collectionDateText = collectionDate is { } d ? IsoDatePattern.Format(d) : string.Empty;

            var cardErrorDescription =
                $"<b>We couldn't charge your {cardBrand} card ending with {cardLast4}, because {cardErrorReason}.</b> " +
                $"<p>Please use the secure link below to complete the payment for your quotation {quote.QuoteReference} with Tranzr Moves.</p> " +
                "To avoid any delay or cancellation of your scheduled service on " +
                $"{collectionDateText}, kindly make your payment as soon as possible or contact us to arrange an alternative payment option.";

            var checkout = await hostedCheckoutSessionService.CreateAsync(
                quote,
                quote.TotalCost!.Value,
                $"Full payment for quote #{quote.QuoteReference}",
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
            return Error.Failure("PayLater.Recovery", inner.Message);
        }
    }

    private static string MapCardErrorReason(StripeException ex)
    {
        return ex.StripeError.Code switch
        {
            "card_declined" when ex.StripeError.DeclineCode == "insufficient_funds" =>
                "the card was declined due to insufficient funds",
            "card_declined" => "the card was declined.",
            "expired_card" => "the card has expired.",
            "incorrect_cvc" => "the card's security code is incorrect.",
            "incorrect_number" => "the card number is incorrect.",
            "invalid_cvc" => "the card's security code is invalid.",
            "invalid_expiry_month" => "the expiry month is invalid.",
            "invalid_expiry_year" => "the expiry year is invalid.",
            "processing_error" => "an error occurred while processing the card.",
            "incorrect_zip" => "the card's post code failed validation.",
            "authentication_required" => "the card's authorization is required",
            "approve_with_id" => "the payment cannot be authorized",
            "call_issuer" => "the card has been declined, call issuer",
            "do_not_honor" => "the card has been declined",
            "insufficient_funds" => "the card has insufficient funds.",
            "invalid_account" => "the card or account is invalid.",
            "currency_not_supported" => "the card does not support this currency.",
            "lost_card" => "the card has been reported lost.",
            "stolen_card" => "the card has been reported stolen.",
            _ => ex.StripeError.Message ?? "the payment failed."
        };
    }
}
