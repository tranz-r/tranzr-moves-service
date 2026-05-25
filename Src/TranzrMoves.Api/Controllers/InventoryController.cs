// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Inventory;
using TranzrMoves.Application.Features.Inventory.Categories;
using TranzrMoves.Application.Features.Inventory.Goods;
using TranzrMoves.Application.Features.Inventory.Search;

namespace TranzrMoves.Api.Controllers;

[ApiVersion(1)]
[ApiVersion(2)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class InventoryController(IMediator mediator) : ApiControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> ImportInventoryGoods([FromBody] InventoryImportDto dto, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ImportInventoriesCommand(dto), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [MapToApiVersion(1)]
    [HttpGet("goods")]
    public async Task<IActionResult> GetGoods(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GoodsQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [MapToApiVersion(1)]
    [HttpGet("categories/{categoryId:int}/goods")]
    public async Task<IActionResult> GetGoodsByCategoryId(int categoryId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GoodsByCategoryIdQuery(categoryId), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [MapToApiVersion(1)]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CategoriesQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [MapToApiVersion(1)]
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new InventorySearchQuery(query ?? string.Empty, limit), cancellationToken);
        return result.Match(Ok, Problem);
    }
}
