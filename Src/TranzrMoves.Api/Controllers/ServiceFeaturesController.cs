using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Features.ServiceFeatures.Create;
using TranzrMoves.Application.Features.ServiceFeatures.Delete;
using TranzrMoves.Application.Features.ServiceFeatures.Get;
using TranzrMoves.Application.Features.ServiceFeatures.List;
using TranzrMoves.Application.Features.ServiceFeatures.Update;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class ServiceFeaturesController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListServiceFeaturesQuery(isActive), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetServiceFeatureQuery(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceFeatureCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.Match(serviceFeature => CreatedAtAction(nameof(Get), new { id = serviceFeature.Id }, serviceFeature),
            Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceFeatureCommand command, CancellationToken cancellationToken)
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
        var result = await mediator.Send(new DeleteServiceFeatureCommand(id), cancellationToken);
        return result.Match(_ => NoContent(), Problem);
    }
}