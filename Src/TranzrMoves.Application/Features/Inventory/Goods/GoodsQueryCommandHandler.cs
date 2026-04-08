// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Inventory.Goods;

internal sealed class GoodsQueryCommandHandler(IInventoryItemRepository inventoryItemRepository) : IQueryHandler<GoodsQuery, ErrorOr<List<InventoryGoodImportDto>>>
{
    public async ValueTask<ErrorOr<List<InventoryGoodImportDto>>> Handle(GoodsQuery _, CancellationToken cancellationToken)
    {
        //Map inventory categories
        var result = await inventoryItemRepository.GetAllGoodsAsync(cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var mapper = new InventoryMapper();
        var inventoryGoodsInDb = mapper.ToDtoList(result.Value);

        return inventoryGoodsInDb;
    }
}
