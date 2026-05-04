using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Models;

namespace TranzrMoves.Application.Features.Checkout.DepositBalance;

public sealed record CreateQuoteV2DepositBalancePaymentCommand(FuturePaymentRequest Request)
    : ICommand<ErrorOr<StripeIntentClientSecret>>;
