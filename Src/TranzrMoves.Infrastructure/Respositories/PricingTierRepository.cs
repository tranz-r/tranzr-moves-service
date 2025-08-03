using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class PricingTierRepository(TranzrMovesDbContext dbContext, ILogger<PricingTierRepository> logger) : IPricingTierRepository
{
    public async Task<ErrorOr<PricingTier>> AddPricingTierAsync(PricingTier pricingTier,
        CancellationToken cancellationToken)
    {
        dbContext.Set<PricingTier>().Add(pricingTier);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (CannotInsertNullException e)
        {
            logger.LogError("Cannot insert null value for {property}", e.Source);
            return Error.Custom(
                type: (int)CustomErrorType.BadRequest,
                code: "Null.Value",
                description: "Cannot insert null value");
        }
        catch (UniqueConstraintException e)
        {
            logger.LogError("Unique constraint {constraintName} violated. Duplicate value for {constraintProperty}",
                e.ConstraintName, e.ConstraintProperties[0]);
            return Error.Conflict();
        }

        return pricingTier;
    }

    public async Task<PricingTier?> GetPricingTierAsync(Guid pricingTierId, CancellationToken cancellationToken)
        => await dbContext.Set<PricingTier>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == pricingTierId, cancellationToken);


    public async Task<ErrorOr<PricingTier>> UpdatePricingTierAsync(PricingTier pricingTier,
        CancellationToken cancellationToken)
    {
        dbContext.Set<PricingTier>().Update(pricingTier);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating PricingTier with PricingTierId {PricingTierId}",
                pricingTier.Id);
            return Error.Conflict();
        }

        return pricingTier;
    }

    public async Task DeletePricingTierAsync(PricingTier pricingTier, CancellationToken cancellationToken)
        => await dbContext.Set<PricingTier>()
            .Where(ac => ac.Id == pricingTier.Id)
            .ExecuteDeleteAsync(cancellationToken);
}