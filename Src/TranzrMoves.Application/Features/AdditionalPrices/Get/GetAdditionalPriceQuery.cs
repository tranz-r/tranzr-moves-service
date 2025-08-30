using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.AdditionalPrices.Get;

public record GetAdditionalPriceQuery(Guid Id) : IRequest<ErrorOr<AdditionalPriceDto>>;
