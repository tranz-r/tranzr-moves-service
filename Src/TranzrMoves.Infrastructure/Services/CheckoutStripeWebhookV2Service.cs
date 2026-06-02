using System.Globalization;
using ErrorOr;
using Microsoft.Extensions.Logging;
using NodaTime.Text;
using Stripe;
using Stripe.Checkout;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Statics;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using FromEmails = TranzrMoves.Domain.Constants.FromEmails;

namespace TranzrMoves.Infrastructure.Services;

public sealed class CheckoutStripeWebhookV2Service(
    StripeClient stripeClient,
    string webhookSigningSecretV2,
    IQuoteRepository quoteRepository,
    IQuoteProgressCalculator progressCalculator,
    INotificationPublisher notificationPublisher,
    ITimeService timeService,
    IBalanceChargeScheduler balanceChargeScheduler,
    ILogger<CheckoutStripeWebhookV2Service> logger) : ICheckoutStripeWebhookV2Service
{
    private static readonly LocalDatePattern EmailDatePattern =
        LocalDatePattern.CreateWithInvariantCulture("dddd, MMMM dd, yyyy");

    private static readonly LocalTimePattern EmailTimePattern =
        LocalTimePattern.CreateWithInvariantCulture("HH:mm");

    public async Task<ErrorOr<Success>> ProcessAsync(string rawJson, string stripeSignature,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(webhookSigningSecretV2))
        {
            return Error.Validation("Stripe.WebhookV2.Secret", "TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2 is not set.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(rawJson, stripeSignature, webhookSigningSecretV2);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe webhook v2 signature verification failed");
            return Error.Failure("Stripe.WebhookV2.Signature", ex.Message);
        }

        try
        {
            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded &&
                stripeEvent.Data.Object is PaymentIntent paymentIntent)
            {
                await HandlePaymentIntentSucceededV2Async(paymentIntent, cancellationToken);
            }
            else if (stripeEvent.Type == EventTypes.SetupIntentSucceeded &&
                     stripeEvent.Data.Object is SetupIntent setupIntent)
            {
                await HandleSetupIntentSucceededV2Async(setupIntent, cancellationToken);
            }
            else if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted &&
                     stripeEvent.Data.Object is Session session)
            {
                await HandleCheckoutSessionCompletedV2Async(session, cancellationToken);
            }
            else
            {
                logger.LogWarning("Stripe webhook v2 unhandled event type: {EventType}", stripeEvent.Type);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe webhook v2 handler error for {EventType}", stripeEvent.Type);
            // Return success to avoid Stripe infinite retries for transient failures; logged above.
        }

        return Result.Success;
    }

    private async Task HandleSetupIntentSucceededV2Async(SetupIntent setupIntent, CancellationToken ct)
    {
        if (!setupIntent.Metadata.TryGetValue(nameof(PaymentMetadata.QuoteId), out var quoteIdStr) ||
            !Guid.TryParse(quoteIdStr, out var quoteId))
        {
            return;
        }

        if (!setupIntent.Metadata.TryGetValue(nameof(PaymentMetadata.PaymentType), out var paymentTypeStr) ||
            paymentTypeStr != nameof(PaymentType.Later))
        {
            return;
        }

        var quote = await quoteRepository.GetQuoteByIdAsync(quoteId, ct, true);
        if (quote?.Customer?.Email is null)
        {
            logger.LogWarning("QuoteV2 {QuoteId} not found or has no customer for setup intent {SetupIntentId}",
                quoteId, setupIntent.Id);
            return;
        }

        var payment = quote.Payments?.FirstOrDefault(p =>
            p.SetupIntentId == setupIntent.Id && p.PaymentType == PaymentType.Later);
        if (payment is null)
        {
            logger.LogWarning("No Later payment row for SetupIntent {SetupIntentId} on quote {QuoteId}",
                setupIntent.Id, quoteId);
            return;
        }

        payment.PaymentMethodId = setupIntent.PaymentMethodId;
        payment.ModifiedAt = timeService.Now();
        payment.ModifiedBy = nameof(CheckoutStripeWebhookV2Service);
        quote.PaymentStatus = PaymentStatus.PaymentSetup;

        RecalculateStepState(quote!, QuoteSteps.Payment, QuoteStepKeys.Payment);

        var setupInstant = timeService.Now();
        var totalCost = decimal.Parse(
            setupIntent.Metadata.GetValueOrDefault(nameof(PaymentMetadata.TotalCost), "0"),
            CultureInfo.InvariantCulture);

        var customerName = $"{quote.Customer.FirstName?.Trim()} {quote.Customer.LastName?.Trim()}".Trim();
        if (string.IsNullOrEmpty(customerName))
        {
            customerName = quote.Customer.Email;
        }

        var templateData = new
        {
            customerName,
            totalAmount = totalCost.ToString("N2", CultureInfo.InvariantCulture),
            amountChargedToday = "0.00",
            paymentDueDate = payment.DueDate is { } dueDateForEmail
                ? EmailDatePattern.Format(dueDateForEmail)
                : null,
            quoteReference = quote.QuoteReference,
            setupDate = EmailDatePattern.Format(setupInstant.InUtc().Date),
            setupTime = EmailTimePattern.Format(setupInstant.InUtc().TimeOfDay) + " GMT",
            currentYear = timeService.NowInUtc().Year
        };

        await notificationPublisher.PublishAsync(
            NotificationPublishHelper.Create(
                Guid.NewGuid(),
                setupIntent.Id,
                "setup-confirmation",
                quote.Customer.Email,
                FromEmails.Booking,
                $"Payment Setup Confirmation - #{quote.QuoteReference}",
                templateData),
            ct);

        var save = await quoteRepository.SaveChangesAsync(ct);
        if (save.IsError)
        {
            logger.LogWarning("Save failed after setup intent for quote {QuoteId}", quoteId);
            return;
        }

        if (payment.DueDate is { } dueDate)
        {
            await balanceChargeScheduler.SchedulePayLaterAsync(quote.Id, dueDate, quote.QuoteReference, ct);
        }
    }

    private async Task HandlePaymentIntentSucceededV2Async(PaymentIntent paymentIntent, CancellationToken ct)
    {
        if (!paymentIntent.Metadata.TryGetValue(nameof(PaymentMetadata.QuoteId), out var quoteIdStr) ||
            !Guid.TryParse(quoteIdStr, out var quoteId))
        {
            return;
        }

        if (!paymentIntent.Metadata.TryGetValue(nameof(PaymentMetadata.PaymentType), out var paymentTypeStr))
        {
            return;
        }

        var quote = await quoteRepository.GetQuoteByIdAsync(quoteId, ct, true);
        if (quote?.Customer?.Email is null)
        {
            return;
        }

        var paymentRow = quote.Payments?.FirstOrDefault(p =>
                             p.PaymentIntentId == paymentIntent.Id
                             && (p.CustomerSelectedOption || p.PaymentType == PaymentType.Balance)
                             );

        if (paymentRow is null)
        {
            logger.LogWarning("No payment row for PI {PaymentIntentId} on QuoteV2 {QuoteId}", paymentIntent.Id,
                quoteId);
            return;
        }

        string? receiptUrl = null;
        if (!string.IsNullOrEmpty(paymentIntent.LatestChargeId))
        {
            try
            {
                var charge = await stripeClient.V1.Charges.GetAsync(paymentIntent.LatestChargeId,
                    cancellationToken: ct);
                receiptUrl = charge.ReceiptUrl;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load charge for PI {PaymentIntentId}", paymentIntent.Id);
            }
        }

        paymentRow.ReceiptUrl = receiptUrl;
        paymentRow.PaymentMethodId ??= paymentIntent.PaymentMethodId;
        paymentRow.Status = StripePaymentStatus.Paid;
        paymentRow.ModifiedAt = timeService.Now();
        paymentRow.ModifiedBy = nameof(CheckoutStripeWebhookV2Service);

        if (paymentTypeStr == nameof(PaymentType.Balance))
        {
            quote.PaymentStatus = PaymentStatus.Paid;
        }
        else if (paymentTypeStr == nameof(PaymentType.Deposit))
        {
            quote.PaymentStatus = PaymentStatus.PartiallyPaid;
        }
        else
        {
            quote.PaymentStatus = PaymentStatus.Paid;
        }

        RecalculateStepState(quote!, QuoteSteps.Payment, QuoteStepKeys.Payment);

        var orderInstant = timeService.Now();
        var customerName = $"{quote.Customer.FirstName?.Trim()} {quote.Customer.LastName?.Trim()}".Trim();
        if (string.IsNullOrEmpty(customerName))
        {
            customerName = quote.Customer.Email;
        }

        if (paymentTypeStr == nameof(PaymentType.Balance))
        {
            var totalCost = quote.TotalCost ?? (paymentIntent.Amount / 100m);
            var templateData = new
            {
                customerName,
                balanceAmount = (paymentIntent.Amount / 100m).ToString("N2", CultureInfo.InvariantCulture),
                totalAmount = totalCost.ToString("N2", CultureInfo.InvariantCulture),
                quoteReference = quote.QuoteReference,
                paymentDate = EmailDatePattern.Format(orderInstant.InUtc().Date),
                paymentTime = EmailTimePattern.Format(orderInstant.InUtc().TimeOfDay) + " GMT",
                currentYear = timeService.NowInUtc().Year,
                extraCharges = (string?)null,
                extraChargesDescription = (string?)null
            };

            await notificationPublisher.PublishAsync(
                NotificationPublishHelper.Create(
                    Guid.NewGuid(),
                    paymentIntent.Id,
                    "balance-confirmation",
                    quote.Customer.Email,
                    FromEmails.Booking,
                    $"Balance Payment Confirmation - #{quote.QuoteReference}",
                    templateData),
                ct);
        }
        else if (paymentTypeStr == nameof(PaymentType.Deposit))
        {
            var depositAmount = paymentIntent.Amount / 100m;
            var totalCost = quote.TotalCost ?? depositAmount;
            var remainingAmount = totalCost - depositAmount;
            var templateData = new
            {
                customerName,
                depositAmount = depositAmount.ToString("N2", CultureInfo.InvariantCulture),
                totalAmount = totalCost.ToString("N2", CultureInfo.InvariantCulture),
                remainingAmount = remainingAmount.ToString("N2", CultureInfo.InvariantCulture),
                quoteReference = quote.QuoteReference,
                paymentDate = EmailDatePattern.Format(orderInstant.InUtc().Date),
                paymentTime = EmailTimePattern.Format(orderInstant.InUtc().TimeOfDay) + " GMT",
                currentYear = timeService.NowInUtc().Year
            };

            await notificationPublisher.PublishAsync(
                NotificationPublishHelper.Create(
                    Guid.NewGuid(),
                    paymentIntent.Id,
                    "deposit-confirmation",
                    quote.Customer.Email,
                    FromEmails.Booking,
                    $"Deposit Confirmation - #{quote.QuoteReference}",
                    templateData),
                ct);
        }
        else
        {
            var fullAmount = paymentIntent.Amount / 100m;
            var templateData = new
            {
                customerName,
                totalAmount = fullAmount.ToString("N2", CultureInfo.InvariantCulture),
                quoteReference = quote.QuoteReference,
                paymentDate = EmailDatePattern.Format(orderInstant.InUtc().Date),
                paymentTime = EmailTimePattern.Format(orderInstant.InUtc().TimeOfDay) + " GMT",
                currentYear = timeService.NowInUtc().Year
            };

            await notificationPublisher.PublishAsync(
                NotificationPublishHelper.Create(
                    Guid.NewGuid(),
                    paymentIntent.Id,
                    "full-payment-confirmation",
                    quote.Customer.Email,
                    FromEmails.Booking,
                    $"Order Confirmation - #{quote.QuoteReference}",
                    templateData),
                ct);
        }

        var save = await quoteRepository.SaveChangesAsync(ct);
        if (save.IsError)
        {
            logger.LogWarning("Save failed after PI success for quote {QuoteId}", quoteId);
            return;
        }

        if (paymentTypeStr == nameof(PaymentType.Deposit))
        {
            var collectionDate = quote.Schedule?.CollectionDate?.InUtc().Date
                                 ?? paymentRow.DueDate;
            if (collectionDate is { } moveDate)
            {
                await balanceChargeScheduler.ScheduleDepositBalanceAsync(quote.Id, moveDate, quote.QuoteReference, ct);
            }
            else
            {
                logger.LogWarning(
                    "Deposit paid for quote {QuoteId} but no collection date; deposit balance charge not scheduled",
                    quoteId);
            }
        }
    }

    private void RecalculateStepState(QuoteV2 quote, QuoteSteps justPatchedStep, string justPatchedStepKey)
    {
        quote.StepsCompleted = progressCalculator.CalculateCompletedSteps(quote);

        if ((quote.StepsCompleted & justPatchedStep) == justPatchedStep)
        {
            quote.LastCompletedStepKey = justPatchedStepKey;
        }
    }

    private async Task HandleCheckoutSessionCompletedV2Async(Session session, CancellationToken ct)
    {
        if (!session.Metadata.TryGetValue(nameof(PaymentMetadata.QuoteId), out var quoteIdStr) ||
            !Guid.TryParse(quoteIdStr, out var quoteId))
        {
            return;
        }

        if (!session.Metadata.TryGetValue(nameof(PaymentMetadata.CustomerEmail), out var customerEmail) ||
            string.IsNullOrWhiteSpace(customerEmail))
        {
            return;
        }

        var quote = await quoteRepository.GetQuoteByIdAsync(quoteId, ct, true);
        if (quote is null)
        {
            return;
        }

        var paymentRow = quote.Payments?.FirstOrDefault(p => p.StripeSessionId == session.Id);
        if (paymentRow is null)
        {
            logger.LogWarning("No payment with StripeSessionId {SessionId} for QuoteV2 {QuoteId}", session.Id,
                quoteId);
            return;
        }

        decimal paymentAmount;
        if (session.Metadata.TryGetValue(nameof(PaymentMetadata.PaymentAmount), out var amountStr) &&
            decimal.TryParse(amountStr, CultureInfo.InvariantCulture, out var parsed))
        {
            paymentAmount = parsed;
        }
        else
        {
            paymentAmount = 0m;
            if (session.LineItems?.Data is not null)
            {
                foreach (var item in session.LineItems.Data)
                {
                    paymentAmount += item.AmountTotal / 100m;
                }
            }
        }

        string? receiptUrl = null;
        string? paymentMethodId = null;
        if (!string.IsNullOrEmpty(session.PaymentIntentId))
        {
            try
            {
                var pi = await stripeClient.V1.PaymentIntents.GetAsync(session.PaymentIntentId,
                    cancellationToken: ct);
                paymentMethodId = pi.PaymentMethodId;
                if (!string.IsNullOrEmpty(pi.LatestChargeId))
                {
                    var charge = await stripeClient.V1.Charges.GetAsync(pi.LatestChargeId, cancellationToken: ct);
                    receiptUrl = charge.ReceiptUrl;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load PI for checkout session {SessionId}", session.Id);
            }
        }

        paymentRow.PaymentIntentId = session.PaymentIntentId;
        paymentRow.PaymentMethodId ??= paymentMethodId;
        paymentRow.ReceiptUrl = receiptUrl;
        paymentRow.Status = StripePaymentStatus.Paid;
        paymentRow.ModifiedAt = timeService.Now();
        paymentRow.ModifiedBy = nameof(CheckoutStripeWebhookV2Service);
        quote.PaymentStatus = PaymentStatus.Paid;

        var paymentInstant = timeService.Now();
        var user = quote.Customer;
        var customerName = $"{user!.FirstName?.Trim()} {user.LastName?.Trim()}".Trim();
        if (string.IsNullOrEmpty(customerName))
        {
            customerName = user.Email ?? customerEmail;
        }

        var description = session.Metadata.TryGetValue(nameof(PaymentMetadata.ExtraChargesDescription), out var desc)
            ? desc + $" for quote #{quote.QuoteReference}"
            : $"Payment for quote #{quote.QuoteReference}";

        var templateData = new
        {
            customerName,
            paymentAmount = paymentAmount.ToString("N2", CultureInfo.InvariantCulture),
            quoteReference = quote.QuoteReference,
            paymentDate = EmailDatePattern.Format(paymentInstant.InUtc().Date),
            paymentTime = EmailTimePattern.Format(paymentInstant.InUtc().TimeOfDay) + " GMT",
            description,
            receiptUrl,
            currentYear = timeService.NowInUtc().Year
        };

        await notificationPublisher.PublishAsync(
            NotificationPublishHelper.Create(
                Guid.NewGuid(),
                session.Id,
                "checkout-session-completed",
                customerEmail,
                FromEmails.Booking,
                $"Payment Confirmation - #{quote.QuoteReference}",
                templateData),
            ct);

        var save = await quoteRepository.SaveChangesAsync(ct);
        if (save.IsError)
        {
            logger.LogWarning("Save failed after checkout session for quote {QuoteId}", quoteId);
        }
    }
}
