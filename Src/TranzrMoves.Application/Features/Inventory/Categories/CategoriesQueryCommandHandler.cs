// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Inventory.Categories;

internal sealed class CategoriesQueryCommandHandler(IInventoryItemRepository inventoryItemRepository) : IQueryHandler<CategoriesQuery, ErrorOr<List<InventoryCategoryImportDto>>>
{
    public async ValueTask<ErrorOr<List<InventoryCategoryImportDto>>> Handle(CategoriesQuery
        query, CancellationToken cancellationToken)
    {
        var result = await inventoryItemRepository.GetAllCategoriesAsync(cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var mapper = new InventoryMapper();
        var inventoryCategoryInDb = mapper.ToDtoList(result.Value);

        return inventoryCategoryInDb;
    }
}
