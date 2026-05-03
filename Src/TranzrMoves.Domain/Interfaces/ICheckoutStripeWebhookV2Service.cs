using ErrorOr;

namespace TranzrMoves.Domain.Interfaces;

public interface ICheckoutStripeWebhookV2Service
{
    Task<ErrorOr<Success>> ProcessAsync(string rawJson, string stripeSignature, CancellationToken cancellationToken);
}
