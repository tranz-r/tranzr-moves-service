using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Features.AdditionalPrices.Create;
using TranzrMoves.Application.Features.AdditionalPrices.Delete;
using TranzrMoves.Application.Features.AdditionalPrices.Get;
using TranzrMoves.Application.Features.AdditionalPrices.List;
using TranzrMoves.Application.Features.AdditionalPrices.Update;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class AdditionalPricesController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListAdditionalPricesQuery(isActive), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAdditionalPriceQuery(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdditionalPriceCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.Match(
            additionalPrice => CreatedAtAction(nameof(Get), new { id = additionalPrice.Id }, additionalPrice), Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdditionalPriceCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("ID in URL does not match ID in request body");
        }

        var result = await mediator.Send(command, cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAdditionalPriceCommand(id), cancellationToken);
        return result.Match(_ => NoContent(), Problem);
    }
}