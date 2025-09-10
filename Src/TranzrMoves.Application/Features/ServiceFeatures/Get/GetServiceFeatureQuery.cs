using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.ServiceFeatures.Get;

public record GetServiceFeatureQuery(Guid Id) : IRequest<ErrorOr<ServiceFeatureDto>>;
