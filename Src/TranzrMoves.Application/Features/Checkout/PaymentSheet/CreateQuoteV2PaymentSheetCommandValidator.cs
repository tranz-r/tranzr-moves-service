using FluentValidation;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.Checkout.PaymentSheet;

public sealed class CreateQuoteV2PaymentSheetCommandValidator : AbstractValidator<CreateQuoteV2PaymentSheetCommand>
{
    public CreateQuoteV2PaymentSheetCommandValidator()
    {
        RuleFor(x => x.QuoteId)
            .NotEmpty();

        RuleFor(x => x.PaymentType)
            .Must(x => x is PaymentType.Full or PaymentType.Deposit or PaymentType.Later)
            .WithMessage("QuoteV2 payment sheet supports only Full, Deposit, or Later payment types.");
    }
}
