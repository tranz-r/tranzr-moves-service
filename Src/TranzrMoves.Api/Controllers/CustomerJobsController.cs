using System.Collections.Immutable;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Features.CustomerJobs.List;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/customer-jobs")]
public class CustomerJobsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid customerId, [FromQuery] IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken)
    {
        if (customerId == Guid.Empty)
        {
            return BadRequest("customerId is required");
        }
        var result = await mediator.Send(new ListCustomerJobsQuery(customerId, statuses), cancellationToken);
        return Ok(result);
    }
}
