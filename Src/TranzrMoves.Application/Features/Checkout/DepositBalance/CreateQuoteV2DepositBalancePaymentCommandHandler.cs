using Mediator;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.DepositBalance;

public sealed class CreateQuoteV2DepositBalancePaymentCommandHandler(
    IQuoteRepository quoteRepository,
    IQuoteV2DepositBalancePaymentService depositBalancePaymentService)
    : ICommandHandler<CreateQuoteV2DepositBalancePaymentCommand, ErrorOr<StripeIntentClientSecret>>
{
    public async ValueTask<ErrorOr<StripeIntentClientSecret>> Handle(
        CreateQuoteV2DepositBalancePaymentCommand command,
        CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetQuoteV2ByQuoteReferenceAsync(command.Request.QuoteReference,
            cancellationToken);

        if (quote is null)
        {
            return Error.NotFound("QuoteV2.NotFound", "QuoteV2 not found.");
        }

        return await depositBalancePaymentService.CreateDepositBalanceAsync(
            quote,
            command.Request.ExtraCharges,
            command.Request.ExtraChargesDescription,
            cancellationToken);
    }
}
