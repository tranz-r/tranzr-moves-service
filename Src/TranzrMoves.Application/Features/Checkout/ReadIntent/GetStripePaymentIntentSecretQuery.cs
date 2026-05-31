using Mediator;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.ReadIntent;

public sealed record GetStripePaymentIntentSecretQuery(string PaymentIntentOrSetupIntentId)
    : IQuery<ErrorOr<StripeIntentClientSecret>>;
