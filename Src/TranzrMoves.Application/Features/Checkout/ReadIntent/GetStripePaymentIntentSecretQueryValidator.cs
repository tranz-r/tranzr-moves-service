using FluentValidation;

namespace TranzrMoves.Application.Features.Checkout.ReadIntent;

public sealed class GetStripePaymentIntentSecretQueryValidator : AbstractValidator<GetStripePaymentIntentSecretQuery>
{
    public GetStripePaymentIntentSecretQueryValidator()
    {
        RuleFor(x => x.PaymentIntentOrSetupIntentId)
            .NotEmpty();
    }
}
