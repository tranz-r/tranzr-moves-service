using Mediator;

namespace TranzrMoves.Application.Features.Checkout.Webhook;

public sealed record ProcessCheckoutStripeWebhookV2Command(
    string RawJson,
    string StripeSignature) : ICommand<ErrorOr<Success>>;
