using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IServiceFeatureRepository
{
    Task<ErrorOr<ServiceFeature>> AddServiceFeatureAsync(ServiceFeature serviceFeature, CancellationToken cancellationToken);
    Task<ServiceFeature?> GetServiceFeatureAsync(Guid id, CancellationToken cancellationToken);
    Task<List<ServiceFeature>> GetServiceFeaturesAsync(bool? isActive, CancellationToken cancellationToken);
    Task<ErrorOr<ServiceFeature>> UpdateServiceFeatureAsync(ServiceFeature serviceFeature, CancellationToken cancellationToken);
    Task DeleteServiceFeatureAsync(ServiceFeature serviceFeature, CancellationToken cancellationToken);
}
