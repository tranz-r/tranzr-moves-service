using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IPricingTierRepository
{
    Task<ErrorOr<PricingTier>> AddPricingTierAsync(PricingTier pricingTier,
        CancellationToken cancellationToken);

    Task<PricingTier?> GetPricingTierAsync(Guid pricingTierId, CancellationToken cancellationToken);

    Task<ErrorOr<PricingTier>> UpdatePricingTierAsync(PricingTier pricingTier,
        CancellationToken cancellationToken);

    Task DeletePricingTierAsync(PricingTier pricingTier, CancellationToken cancellationToken);
}