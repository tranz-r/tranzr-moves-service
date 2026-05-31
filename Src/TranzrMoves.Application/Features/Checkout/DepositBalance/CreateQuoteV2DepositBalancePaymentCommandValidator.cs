using FluentValidation;

namespace TranzrMoves.Application.Features.Checkout.DepositBalance;

public sealed class CreateQuoteV2DepositBalancePaymentCommandValidator
    : AbstractValidator<CreateQuoteV2DepositBalancePaymentCommand>
{
    public CreateQuoteV2DepositBalancePaymentCommandValidator()
    {
        RuleFor(x => x.Request.QuoteReference)
            .NotEmpty()
            .WithMessage("Quote reference is required.");
    }
}
