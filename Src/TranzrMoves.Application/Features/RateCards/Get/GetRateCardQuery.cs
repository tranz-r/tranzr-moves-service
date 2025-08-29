using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.RateCards.Get;

public record GetRateCardQuery(Guid Id) : IRequest<ErrorOr<RateCardDto>>;
