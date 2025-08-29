using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.RateCards.List;

public record ListRateCardsQuery(bool? IsActive) : IRequest<ErrorOr<List<RateCardDto>>>;
