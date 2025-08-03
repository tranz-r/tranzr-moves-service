using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class InventoryItemRepository(TranzrMovesDbContext dbContext, ILogger<InventoryItemRepository> logger) : IInventoryItemRepository
{
    public async Task<ErrorOr<InventoryItem>> AddInventoryItemAsync(InventoryItem inventoryItem,
        CancellationToken cancellationToken)
    {
        dbContext.Set<InventoryItem>().Add(inventoryItem);

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

        return inventoryItem;
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(Guid inventoryItemId, CancellationToken cancellationToken)
        => await dbContext.Set<InventoryItem>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == inventoryItemId, cancellationToken);


    public async Task<ErrorOr<InventoryItem>> UpdateInventoryItemAsync(InventoryItem inventoryItem,
        CancellationToken cancellationToken)
    {
        dbContext.Set<InventoryItem>().Update(inventoryItem);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating InventoryItem with InventoryItemId {InventoryItemId}",
                inventoryItem.Id);
            return Error.Conflict();
        }

        return inventoryItem;
    }

    public async Task DeleteInventoryItemAsync(InventoryItem inventoryItem, CancellationToken cancellationToken)
        => await dbContext.Set<InventoryItem>()
            .Where(ac => ac.Id == inventoryItem.Id)
            .ExecuteDeleteAsync(cancellationToken);
}