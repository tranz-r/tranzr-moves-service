using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.ServiceFeatures.List;

public record ListServiceFeaturesQuery(bool? IsActive) : IRequest<ErrorOr<List<ServiceFeatureDto>>>;
