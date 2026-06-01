using ErrorOr;
using Stripe;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Infrastructure.Services;

public sealed class QuoteV2DepositBalancePaymentService(
    StripeClient stripeClient,
    IQuoteV2DepositBalanceCollectionService depositBalanceCollectionService) : IQuoteV2DepositBalancePaymentService
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

        var result = await depositBalanceCollectionService.CollectAsync(
            quote,
            extraCharges,
            extraChargesDescription,
            ct);

        if (result.IsError)
        {
            return result.Errors;
        }

        var balancePayment = quote.Payments?
            .Where(p => p.PaymentType == PaymentType.Balance)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (balancePayment?.PaymentIntentId is null)
        {
            return Error.Failure("DepositBalance.NoIntent", "Balance payment intent was not created.");
        }

        var paymentIntent = await stripeClient.V1.PaymentIntents.GetAsync(balancePayment.PaymentIntentId, cancellationToken: ct);
        return new StripeIntentClientSecret
        {
            IntentId = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret ?? string.Empty
        };
    }
}
