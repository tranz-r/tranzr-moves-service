using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Contracts.DriverJobs;
using TranzrMoves.Application.Features.DriverJobs.Assign;
using TranzrMoves.Application.Features.DriverJobs.List;
using TranzrMoves.Application.Features.DriverJobs.Unassign;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/driver-jobs")]
public class DriverJobsController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignDriverJobRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AssignDriverJobCommand(request), cancellationToken);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("unassign")]
    public async Task<IActionResult> Unassign([FromBody] UnassignDriverJobRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnassignDriverJobCommand(request), cancellationToken);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid driverId, [FromQuery] IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken)
    {
        if (driverId == Guid.Empty)
        {
            return BadRequest("driverId is required");
        }
        var result = await mediator.Send(new ListDriverJobsQuery(driverId, statuses), cancellationToken);
        return Ok(result);
    }
}
