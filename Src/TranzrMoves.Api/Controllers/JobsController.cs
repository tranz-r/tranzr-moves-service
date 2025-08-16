using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Jobs.Create;
using TranzrMoves.Application.Features.Jobs.GetByQuoteId;
using TranzrMoves.Application.Features.Jobs.ListAll;

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

    [HttpGet]
    public async Task<IActionResult> GetJobAsync([FromQuery] string quoteId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetJobByQuoteIdQuery(quoteId), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllJobsAsync(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListAllJobsQuery(), cancellationToken);
        return Ok(result);
    }
}