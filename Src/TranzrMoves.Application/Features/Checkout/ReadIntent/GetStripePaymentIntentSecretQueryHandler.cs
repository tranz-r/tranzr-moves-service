using Mediator;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.ReadIntent;

public sealed class GetStripePaymentIntentSecretQueryHandler(ICheckoutStripeReadService stripeReadService)
    : IQueryHandler<GetStripePaymentIntentSecretQuery, ErrorOr<StripeIntentClientSecret>>
{
    public async ValueTask<ErrorOr<StripeIntentClientSecret>> Handle(GetStripePaymentIntentSecretQuery query,
        CancellationToken cancellationToken) =>
        await stripeReadService.GetIntentClientSecretAsync(query.PaymentIntentOrSetupIntentId, cancellationToken);
}
