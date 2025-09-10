using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Features.RateCards.Create;
using TranzrMoves.Application.Features.RateCards.Delete;
using TranzrMoves.Application.Features.RateCards.Get;
using TranzrMoves.Application.Features.RateCards.List;
using TranzrMoves.Application.Features.RateCards.Update;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class RateCardsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListRateCardsQuery(isActive), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRateCardQuery(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRateCardCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.Match(
            rateCard => CreatedAtAction(nameof(Get), new { id = rateCard.Id }, rateCard), Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRateCardCommand command, CancellationToken cancellationToken)
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
        var result = await mediator.Send(new DeleteRateCardCommand(id), cancellationToken);
        return result.Match(_ => NoContent(), Problem);
    }
}