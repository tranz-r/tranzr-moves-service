using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.AdditionalPrices.List;

public record ListAdditionalPricesQuery(bool? IsActive) : IRequest<ErrorOr<List<AdditionalPriceDto>>>;
