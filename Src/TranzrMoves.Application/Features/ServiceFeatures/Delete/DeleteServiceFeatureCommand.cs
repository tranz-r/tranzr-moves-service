using ErrorOr;
using Mediator;

namespace TranzrMoves.Application.Features.ServiceFeatures.Delete;

public record DeleteServiceFeatureCommand(Guid Id) : IRequest<ErrorOr<bool>>;
