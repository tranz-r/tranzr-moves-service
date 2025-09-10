using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.ServiceFeatures.Update;

public record UpdateServiceFeatureCommand(
    Guid Id,
    ServiceLevel ServiceLevel,
    string Text,
    int DisplayOrder,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsActive) : IRequest<ErrorOr<ServiceFeatureDto>>;
