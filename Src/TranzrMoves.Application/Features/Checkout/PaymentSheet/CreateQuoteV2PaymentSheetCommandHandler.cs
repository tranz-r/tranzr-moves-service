using Mediator;
using TranzrMoves.Application.Common;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.PaymentSheet;

public sealed class CreateQuoteV2PaymentSheetCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteV2PaymentSheetService paymentSheetService)
    : ICommandHandler<CreateQuoteV2PaymentSheetCommand, ErrorOr<QuoteV2PaymentSheetResult>>
{
    public async ValueTask<ErrorOr<QuoteV2PaymentSheetResult>> Handle(
        CreateQuoteV2PaymentSheetCommand command,
        CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetQuoteByIdAsync(command.QuoteId, cancellationToken, true);
        if (quote is null)
        {
            return Error.NotFound("QuoteV2.NotFound", "QuoteV2 not found.");
        }

        var versionCheck = QuoteV2Concurrency.EnsureExpectedVersion(quote, command.ExpectedVersion);
        if (versionCheck.IsError)
        {
            return versionCheck.Errors;
        }

        return await paymentSheetService.CreateAsync(quote, command.PaymentType, cancellationToken);
    }
}
