using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Constants;
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

    private static string Normalize(string input)
        => input.Trim().ToLowerInvariant();

    public async Task<List<InventorySearchRow>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        var term = Normalize(query);

        if (term.Length < 2)
            return [];

        limit = limit <= 0 ? 10 : Math.Min(limit, 20);

        var startsWithPattern = term + "%";
        var wordStartsWithPattern = "% " + term + "%";

        var useFuzzy = term.Length >= 3;

        var sql = useFuzzy
            ? BuildFuzzySql()
            : BuildPrefixOnlySql();

        var rows = await dbContext.Set<InventorySearchRow>()
            .FromSqlRaw(sql, term, startsWithPattern, wordStartsWithPattern, limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows;
    }

    private static string BuildPrefixOnlySql()
    {
        return $@"
SELECT
    g.""Id"",
    g.""Name"",
    g.""CategoryId"",
    c.""Name"" AS ""CategoryName"",
    g.""PopularityIndex"",
    g.""LengthCm"",
    g.""WidthCm"",
    g.""HeightCm"",
    g.""VolumeM3""
FROM ""{Db.SCHEMA}"".""{Db.Tables.InventoryGoods}"" g
INNER JOIN ""{Db.SCHEMA}"".""{Db.Tables.InventoryCategories}"" c
    ON c.""Id"" = g.""CategoryId""
WHERE
    lower(g.""Name"") = {{0}}
    OR lower(g.""Name"") LIKE {{1}}
    OR g.""SearchText"" LIKE {{2}}
ORDER BY
    CASE
        WHEN lower(g.""Name"") = {{0}} THEN 0
        WHEN lower(g.""Name"") LIKE {{1}} THEN 1
        WHEN g.""SearchText"" LIKE {{2}} THEN 2
        ELSE 3
    END,
    g.""PopularityIndex"" DESC,
    g.""Name"" ASC
LIMIT {{3}}";
    }

    private static string BuildFuzzySql()
    {
        return $@"
SELECT
    g.""Id"",
    g.""Name"",
    g.""CategoryId"",
    c.""Name"" AS ""CategoryName"",
    g.""PopularityIndex"",
    g.""LengthCm"",
    g.""WidthCm"",
    g.""HeightCm"",
    g.""VolumeM3""
FROM ""{Db.SCHEMA}"".""{Db.Tables.InventoryGoods}"" g
INNER JOIN ""{Db.SCHEMA}"".""{Db.Tables.InventoryCategories}"" c
    ON c.""Id"" = g.""CategoryId""
WHERE
    lower(g.""Name"") = {{0}}
    OR lower(g.""Name"") LIKE {{1}}
    OR g.""SearchText"" LIKE {{2}}
    OR {{0}} <<% g.""SearchText""
    OR {{0}} <% g.""SearchText""
ORDER BY
    CASE
        WHEN lower(g.""Name"") = {{0}} THEN 0
        WHEN lower(g.""Name"") LIKE {{1}} THEN 1
        WHEN g.""SearchText"" LIKE {{2}} THEN 2
        WHEN {{0}} <<% g.""SearchText"" THEN 3
        WHEN {{0}} <% g.""SearchText"" THEN 4
        ELSE 5
    END,
    strict_word_similarity({{0}}, g.""SearchText"") DESC,
    word_similarity({{0}}, g.""SearchText"") DESC,
    g.""PopularityIndex"" DESC,
    g.""Name"" ASC
LIMIT {{3}}";
    }
}
