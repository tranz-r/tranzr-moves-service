using ErrorOr;
using Stripe;
using Stripe.Checkout;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Infrastructure.Services;

public sealed class CheckoutStripeReadService(StripeClient stripeClient)
    : ICheckoutStripeReadService
{
    public async Task<ErrorOr<CheckoutStripeSessionSummary>> GetCheckoutSessionAsync(string sessionId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Error.Validation("Checkout.SessionId", "id is required");
        }

        try
        {
            var sessionService = new SessionService(stripeClient);
            var session = await sessionService.GetAsync(sessionId, cancellationToken: ct);
            return new CheckoutStripeSessionSummary
            {
                SessionId = session.Id,
                CustomerId = session.CustomerId,
                PaymentIntentId = session.PaymentIntentId,
                Status = session.Status ?? string.Empty,
                PaymentStatus = session.PaymentStatus ?? string.Empty,
                Url = session.Url ?? string.Empty
            };
        }
        catch (StripeException ex)
        {
            return Error.Failure("Stripe.CheckoutSession", ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<ErrorOr<StripeIntentClientSecret>> GetIntentClientSecretAsync(
        string paymentIntentOrSetupIntentId,
        CancellationToken ct)
    {
        try
        {
            if (paymentIntentOrSetupIntentId.StartsWith("seti_", StringComparison.Ordinal))
            {
                var setupIntent =
                    await stripeClient.V1.SetupIntents.GetAsync(paymentIntentOrSetupIntentId, cancellationToken: ct);
                return new StripeIntentClientSecret
                {
                    ClientSecret = setupIntent.ClientSecret ?? string.Empty,
                    IntentId = setupIntent.Id
                };
            }

            var paymentIntent =
                await stripeClient.V1.PaymentIntents.GetAsync(paymentIntentOrSetupIntentId, cancellationToken: ct);
            return new StripeIntentClientSecret
            {
                ClientSecret = paymentIntent.ClientSecret ?? string.Empty,
                IntentId = paymentIntent.Id
            };
        }
        catch (StripeException ex)
        {
            return Error.Failure("Stripe.Intent", ex.StripeError?.Message ?? ex.Message);
        }
    }
}
