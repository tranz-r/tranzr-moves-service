using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IInventoryItemRepository
{
    Task<ErrorOr<(List<InventoryCategory> InventoryCategories, List<InventoryGood> InventoryGoods)>> ImportInventoryGoodsAsync(List<InventoryCategory> categories, List<InventoryGood> inventoryGoods, CancellationToken cancellationToken);
    Task<ErrorOr<List<InventoryGood>>> GetAllGoodsAsync(CancellationToken cancellationToken);
    Task<ErrorOr<List<InventoryGood>>> GetGoodsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken);
    Task<ErrorOr<List<InventoryCategory>>> GetAllCategoriesAsync(CancellationToken cancellationToken);
}
