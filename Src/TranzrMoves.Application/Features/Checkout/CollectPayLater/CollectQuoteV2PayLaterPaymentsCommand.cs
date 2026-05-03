using Mediator;

namespace TranzrMoves.Application.Features.Checkout.CollectPayLater;

public sealed record CollectQuoteV2PayLaterPaymentsCommand : ICommand<ErrorOr<Success>>;
