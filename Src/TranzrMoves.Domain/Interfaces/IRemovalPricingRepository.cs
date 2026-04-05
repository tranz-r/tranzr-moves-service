using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IRemovalPricingRepository
{
    Task<List<RateCard>> GetRateCardsAsync(Instant at, CancellationToken cancellationToken);
    Task<List<ServiceFeature>> GetServiceFeatureAsync(Instant at, CancellationToken cancellationToken);
}