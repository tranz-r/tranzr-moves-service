using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Application.Features.Jobs.Create;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class JobsController(IMediator  mediator, IJobRepository jobRepository) : ApiControllerBase
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
        if (string.IsNullOrWhiteSpace(quoteId))
        {
            return BadRequest("quoteId is required");
        }
        var job = await jobRepository.GetJobByQuoteIdAsync(quoteId, cancellationToken);
        if (job == null) return NotFound();
        var mapper = new JobMapper();
        var dto = mapper.MapJobToDto(job);
        return Ok(dto);
    }
}