using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Jobs.Create;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class JobsController(IMediator  mediator) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateJobAsync([FromBody] JobDto jobDto)
    {
        var jobCommand = new CreateJobCommand(jobDto);
        var response = await mediator.Send(jobCommand, HttpContext.RequestAborted);
        return response.Match(Ok, Problem);
    }
}