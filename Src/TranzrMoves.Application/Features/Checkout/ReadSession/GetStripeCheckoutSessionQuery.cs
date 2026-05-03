using Mediator;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.ReadSession;

public sealed record GetStripeCheckoutSessionQuery(string SessionId) : IQuery<ErrorOr<CheckoutStripeSessionSummary>>;
