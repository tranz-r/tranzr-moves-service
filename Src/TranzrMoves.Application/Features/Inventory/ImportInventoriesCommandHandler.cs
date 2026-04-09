// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Inventory;

internal sealed class ImportInventoriesCommandHandler(IInventoryItemRepository inventoryItemRepository) : ICommandHandler<ImportInventoriesCommand, ErrorOr<InventoryImportDto>>
{
    public async ValueTask<ErrorOr<InventoryImportDto>> Handle(ImportInventoriesCommand command, CancellationToken cancellationToken)
    {
        //Map inventory categories
        var mapper = new InventoryMapper();

        //Map inventory Goods
        var categories = mapper.ToEntityList(command.InventoryImportDto.Categories);

        var inventoryGoods = mapper.ToEntityList(command.InventoryImportDto.Goods);
        //Pass to repositories

        var result = await inventoryItemRepository.ImportInventoryGoodsAsync(categories, inventoryGoods, cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var (ic, ig) = result.Value;

        var inventoryCategoryInDb = mapper.ToDtoList(ic);
        var inventoryGoodsInDb = mapper.ToDtoList(ig);

        return new InventoryImportDto
        {
            Categories = inventoryCategoryInDb,
            Goods = inventoryGoodsInDb
        };
    }
}
