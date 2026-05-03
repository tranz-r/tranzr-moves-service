using Mediator;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.ReadSession;

public sealed class GetStripeCheckoutSessionQueryHandler(ICheckoutStripeReadService stripeReadService)
    : IQueryHandler<GetStripeCheckoutSessionQuery, ErrorOr<CheckoutStripeSessionSummary>>
{
    public async ValueTask<ErrorOr<CheckoutStripeSessionSummary>> Handle(GetStripeCheckoutSessionQuery query,
        CancellationToken cancellationToken) =>
        await stripeReadService.GetCheckoutSessionAsync(query.SessionId, cancellationToken);
}
