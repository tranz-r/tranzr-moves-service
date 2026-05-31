using ErrorOr;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Domain.Interfaces;

public interface ICheckoutStripeReadService
{
    Task<ErrorOr<CheckoutStripeSessionSummary>> GetCheckoutSessionAsync(string sessionId, CancellationToken ct);

    Task<ErrorOr<StripeIntentClientSecret>> GetIntentClientSecretAsync(string paymentIntentOrSetupIntentId,
        CancellationToken ct);
}
