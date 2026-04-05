using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Prices.Removals;

public record RemovalPricesRequest(Instant At) : IRequest<ErrorOr<RemovalPricingDto>>;
