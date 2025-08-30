using ErrorOr;
using Mediator;

namespace TranzrMoves.Application.Features.AdditionalPrices.Delete;

public record DeleteAdditionalPriceCommand(Guid Id) : IRequest<ErrorOr<bool>>;
