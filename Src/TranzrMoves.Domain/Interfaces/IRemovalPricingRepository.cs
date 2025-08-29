using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IRemovalPricingRepository
{
    Task<List<RateCard>> GetRateCardsAsync(DateTimeOffset at, CancellationToken cancellationToken);
    Task<List<ServiceFeature>> GetServiceFeatureAsync(DateTimeOffset at, CancellationToken cancellationToken);
}