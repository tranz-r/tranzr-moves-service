using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.ServiceFeatures.Create;

public record CreateServiceFeatureCommand(
    ServiceLevel ServiceLevel,
    string Text,
    int DisplayOrder,
    Instant EffectiveFrom,
    Instant? EffectiveTo,
    bool IsActive) : IRequest<ErrorOr<ServiceFeatureDto>>;
