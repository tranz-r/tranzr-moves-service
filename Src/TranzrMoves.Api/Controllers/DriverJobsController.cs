using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Contracts.DriverJobs;
using TranzrMoves.Application.Features.DriverJobs.Assign;
using TranzrMoves.Application.Features.DriverJobs.Unassign;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/driver-jobs")]
public class DriverJobsController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignDriverQuoteRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AssignDriverQuoteCommand(request), cancellationToken);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("unassign")]
    public async Task<IActionResult> Unassign([FromBody] UnassignDriverJobRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnassignDriverJobCommand(request), cancellationToken);
        return result.Match(_ => Ok(), Problem);
    }
}
