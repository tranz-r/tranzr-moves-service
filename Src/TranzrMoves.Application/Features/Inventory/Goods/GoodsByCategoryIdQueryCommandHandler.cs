// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Inventory.Goods;

internal sealed class GoodsByCategoryIdQueryCommandHandler(IInventoryItemRepository inventoryItemRepository) : IQueryHandler<GoodsByCategoryIdQuery, ErrorOr<List<InventoryGoodImportDto>>>
{
    public async ValueTask<ErrorOr<List<InventoryGoodImportDto>>> Handle(GoodsByCategoryIdQuery query, CancellationToken cancellationToken)
    {
        var result = await inventoryItemRepository.GetGoodsByCategoryIdAsync(query.CategoryId, cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var mapper = new InventoryMapper();
        var inventoryGoodsInDb = mapper.ToDtoList(result.Value);

        return inventoryGoodsInDb;
    }
}
