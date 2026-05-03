using FluentValidation;

namespace TranzrMoves.Application.Features.Checkout.ReadSession;

public sealed class GetStripeCheckoutSessionQueryValidator : AbstractValidator<GetStripeCheckoutSessionQuery>
{
    public GetStripeCheckoutSessionQueryValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("id is required");
    }
}
