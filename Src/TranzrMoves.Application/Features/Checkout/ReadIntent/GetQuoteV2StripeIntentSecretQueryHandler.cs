using Mediator;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.ReadIntent;

public sealed class GetQuoteV2StripeIntentSecretQueryHandler(
    IQuoteRepository quoteRepository,
    ICheckoutStripeReadService stripeReadService)
    : IQueryHandler<GetQuoteV2StripeIntentSecretQuery, ErrorOr<StripeIntentClientSecret>>
{
    public async ValueTask<ErrorOr<StripeIntentClientSecret>> Handle(GetQuoteV2StripeIntentSecretQuery query,
        CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetQuoteByIdAsync(query.QuoteId, cancellationToken, false);
        if (quote is null)
        {
            return Error.NotFound("QuoteV2.NotFound", "QuoteV2 not found.");
        }

        var payment = ResolvePaymentWithStripeIntent(quote);
        if (payment is null)
        {
            return Error.NotFound(
                "QuoteV2.NoPaymentIntent",
                "No Stripe payment or setup intent is stored for this quote yet.");
        }

        var intentId = ResolveStripeIntentId(payment);
        if (string.IsNullOrWhiteSpace(intentId))
        {
            return Error.NotFound(
                "QuoteV2.NoPaymentIntent",
                "No Stripe payment or setup intent is stored for this quote yet.");
        }

        return await stripeReadService.GetIntentClientSecretAsync(intentId, cancellationToken);
    }

    private static Payment? ResolvePaymentWithStripeIntent(QuoteV2 quote)
    {
        var payments = quote.Payments;
        if (payments is null || payments.Count == 0)
        {
            return null;
        }

        static bool IsSelectable(PaymentType t) =>
            t is PaymentType.Full or PaymentType.Deposit or PaymentType.Later;

        static bool HasIntent(Payment p) =>
            !string.IsNullOrWhiteSpace(p.PaymentIntentId) || !string.IsNullOrWhiteSpace(p.SetupIntentId);

        var selected = payments
            .Where(p => p.CustomerSelectedOption && IsSelectable(p.PaymentType) && HasIntent(p))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (selected is not null)
        {
            return selected;
        }

        return payments
            .Where(p => IsSelectable(p.PaymentType) && HasIntent(p))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();
    }

    private static string? ResolveStripeIntentId(Payment payment) =>
        payment.PaymentType == PaymentType.Later ? payment.SetupIntentId : payment.PaymentIntentId;
}
