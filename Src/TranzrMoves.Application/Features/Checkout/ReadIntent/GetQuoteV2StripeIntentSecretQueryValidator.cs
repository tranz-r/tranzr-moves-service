using FluentValidation;

namespace TranzrMoves.Application.Features.Checkout.ReadIntent;

public sealed class GetQuoteV2StripeIntentSecretQueryValidator : AbstractValidator<GetQuoteV2StripeIntentSecretQuery>
{
    public GetQuoteV2StripeIntentSecretQueryValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
    }
}
