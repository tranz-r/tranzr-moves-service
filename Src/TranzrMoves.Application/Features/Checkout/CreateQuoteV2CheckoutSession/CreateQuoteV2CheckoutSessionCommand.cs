using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Checkout.CreateQuoteV2CheckoutSession;

public sealed record CreateQuoteV2CheckoutSessionCommand(
    Guid QuoteId,
    uint ExpectedVersion,
    decimal Amount,
    string Description) : ICommand<ErrorOr<CreateQuoteV2CheckoutSessionResponse>>;
