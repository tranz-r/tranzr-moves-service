using ErrorOr;
using Mediator;

namespace TranzrMoves.Application.Features.RateCards.Delete;

public record DeleteRateCardCommand(Guid Id) : IRequest<ErrorOr<bool>>;
