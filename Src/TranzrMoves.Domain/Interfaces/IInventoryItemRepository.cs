using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IInventoryItemRepository
{
    Task<ErrorOr<InventoryItem>> AddInventoryItemAsync(InventoryItem inventoryItem,
        CancellationToken cancellationToken);

    Task<InventoryItem?> GetInventoryItemAsync(Guid inventoryItemId, CancellationToken cancellationToken);

    Task<ErrorOr<InventoryItem>> UpdateInventoryItemAsync(InventoryItem inventoryItem,
        CancellationToken cancellationToken);

    Task DeleteInventoryItemAsync(InventoryItem inventoryItem, CancellationToken cancellationToken);
}