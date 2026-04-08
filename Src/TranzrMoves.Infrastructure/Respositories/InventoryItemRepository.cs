using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class InventoryItemRepository(TranzrMovesDbContext dbContext, ILogger<InventoryItemRepository> logger) : IInventoryItemRepository
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

    public async Task<ErrorOr<(List<InventoryCategory> InventoryCategories, List<InventoryGood> InventoryGoods)>> ImportInventoryGoodsAsync(List<InventoryCategory> categories, List<InventoryGood> inventoryGoods, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            dbContext.Set<InventoryGood>().RemoveRange(dbContext.Set<InventoryGood>());
            dbContext.Set<InventoryCategory>().RemoveRange(dbContext.Set<InventoryCategory>());

            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.Set<InventoryCategory>().AddRangeAsync(categories, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.Set<InventoryGood>().AddRangeAsync(inventoryGoods, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var cat = await dbContext.Set<InventoryCategory>().ToListAsync(cancellationToken);
            var gds = await dbContext.Set<InventoryGood>().ToListAsync(cancellationToken);

            return (cat, gds);
        }
        catch (Exception e)
        {
            return Error.Failure("Inventory.Import.failure", e.Message);
        }
    }

    public async Task<ErrorOr<List<InventoryGood>>> GetAllGoodsAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<InventoryGood>()
            .AsNoTracking()
            .Include(x => x.Category)
            .OrderByDescending(x => x.PopularityIndex)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<ErrorOr<List<InventoryGood>>> GetGoodsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<InventoryGood>()
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.CategoryId == categoryId)
            .OrderByDescending(x => x.PopularityIndex)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<ErrorOr<List<InventoryCategory>>> GetAllCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Set<InventoryCategory>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return categories;
    }
}
