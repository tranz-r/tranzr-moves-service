using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class RemovalPricingRepository(TranzrMovesDbContext db, ILogger<RemovalPricingRepository> logger) : IRemovalPricingRepository
{
    public async Task<List<RateCard>> GetRateCardsAsync(Instant at, CancellationToken cancellationToken)
    {
        return await db.Set<RateCard>()
            .Where(r => r.IsActive && r.EffectiveFrom <= at && (r.EffectiveTo == null || r.EffectiveTo > at))
            .ToListAsync(cancellationToken);
    }
    
    public async Task<List<ServiceFeature>> GetServiceFeatureAsync(Instant at, CancellationToken cancellationToken)
    {
        return await db.Set<ServiceFeature>()
            .Where(f => f.IsActive && f.EffectiveFrom <= at && (f.EffectiveTo == null || f.EffectiveTo > at))
            .OrderBy(f => f.ServiceLevel).ThenBy(f => f.DisplayOrder)
            .ToListAsync(cancellationToken);
    }
}