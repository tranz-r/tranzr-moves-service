using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class RateCardRepository(TranzrMovesDbContext dbContext, ILogger<RateCardRepository> logger) : IRateCardRepository
{
    public async Task<ErrorOr<RateCard>> AddRateCardAsync(RateCard rateCard, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Add(rateCard);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully added rate card {Id} for {Movers} movers with {ServiceLevel} service level", 
                rateCard.Id, rateCard.Movers, rateCard.ServiceLevel);
            
            return rateCard;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add rate card");
            return Error.Failure("RateCard.AddError", "Failed to add rate card to database");
        }
    }

    public async Task<RateCard?> GetRateCardAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Set<RateCard>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<RateCard>> GetRateCardsAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<RateCard>().AsQueryable();
        
        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }
        
        return await query
            .OrderBy(r => r.Movers)
            .ThenBy(r => r.ServiceLevel)
            .ToListAsync(cancellationToken);
    }

    public async Task<ErrorOr<RateCard>> UpdateRateCardAsync(RateCard rateCard, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Update(rateCard);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully updated rate card {Id}", rateCard.Id);
            
            return rateCard;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating rate card {Id}", rateCard.Id);
            return Error.Conflict("RateCard.ConcurrencyConflict", "The rate card was modified by another user");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update rate card {Id}", rateCard.Id);
            return Error.Failure("RateCard.UpdateError", "Failed to update rate card in database");
        }
    }

    public async Task DeleteRateCardAsync(RateCard rateCard, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Remove(rateCard);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully deleted rate card {Id}", rateCard.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete rate card {Id}", rateCard.Id);
            throw;
        }
    }
}
