// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Inventory.Search;

internal sealed class InventorySearchQueryCommandHandler(IInventoryItemRepository inventoryItemRepository)
    : IQueryHandler<InventorySearchQuery, ErrorOr<List<InventoryGoodDto>>>
{
    public async ValueTask<ErrorOr<List<InventoryGoodDto>>> Handle(InventorySearchQuery query, CancellationToken cancellationToken)
    {
        var result = await inventoryItemRepository.SearchAsync(query.Query, query.Limit, cancellationToken);

        var mapper = new InventoryMapper();
        return mapper.ToInventoryGoodDtoList(result);
    }
}
