using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class AdditionalPriceRepository(TranzrMovesDbContext dbContext, ILogger<AdditionalPriceRepository> logger) : IAdditionalPriceRepository
{
    public async Task<ErrorOr<AdditionalPrice>> AddAdditionalPriceAsync(AdditionalPrice additionalPrice, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Add(additionalPrice);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully added additional price {Id} of type {Type}", 
                additionalPrice.Id, additionalPrice.Type);
            
            return additionalPrice;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding additional price");
            return Error.Failure("AdditionalPrice.AddError", "An error occurred while adding the additional price");
        }
    }

    public async Task<AdditionalPrice?> GetAdditionalPriceAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Set<AdditionalPrice>()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<AdditionalPrice>> GetAdditionalPricesAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<AdditionalPrice>().AsQueryable();
        
        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }
        
        return await query
            .OrderBy(x => x.Type)
            .ThenBy(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<ErrorOr<AdditionalPrice>> UpdateAdditionalPriceAsync(AdditionalPrice additionalPrice, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Update(additionalPrice);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully updated additional price {Id}", additionalPrice.Id);
            
            return additionalPrice;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating additional price {Id}", additionalPrice.Id);
            return Error.Conflict("AdditionalPrice.ConcurrencyError", "The additional price was modified by another user");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating additional price {Id}", additionalPrice.Id);
            return Error.Failure("AdditionalPrice.UpdateError", "An error occurred while updating the additional price");
        }
    }

    public async Task DeleteAdditionalPriceAsync(AdditionalPrice additionalPrice, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Remove(additionalPrice);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully deleted additional price {Id}", additionalPrice.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete additional price {Id}", additionalPrice.Id);
            throw;
        }
    }
}
