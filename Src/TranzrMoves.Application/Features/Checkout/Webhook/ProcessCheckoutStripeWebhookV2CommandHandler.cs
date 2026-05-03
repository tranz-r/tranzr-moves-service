using Mediator;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Checkout.Webhook;

public sealed class ProcessCheckoutStripeWebhookV2CommandHandler(ICheckoutStripeWebhookV2Service webhookV2Service)
    : ICommandHandler<ProcessCheckoutStripeWebhookV2Command, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(ProcessCheckoutStripeWebhookV2Command command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RawJson))
        {
            return Error.Validation("Webhook.Body", "Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(command.StripeSignature))
        {
            return Error.Validation("Webhook.Signature", "Stripe-Signature header is required.");
        }

        return await webhookV2Service.ProcessAsync(command.RawJson, command.StripeSignature, cancellationToken);
    }
}
