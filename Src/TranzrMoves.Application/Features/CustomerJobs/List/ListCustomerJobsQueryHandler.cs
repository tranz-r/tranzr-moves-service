using System.Collections.Immutable;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.CustomerJobs.List;

public class ListCustomerJobsQueryHandler(
    ILogger<ListCustomerJobsQueryHandler> logger
) : IQueryHandler<ListCustomerJobsQuery, ImmutableList<JobDto>>
{
    public ValueTask<ImmutableList<JobDto>> Handle(ListCustomerJobsQuery query, CancellationToken cancellationToken)
    {
        // var jobs = await userJobRepository.GetJobsForCustomerAsync(query.CustomerId, query.Statuses, cancellationToken);
        // var mapper = new JobMapper();
        // return jobs.Select(mapper.MapJobToDto).ToImmutableList();

        logger.LogError("ListCustomerJobsQuery is not implemented (customer {CustomerId})", query.CustomerId);
        return ValueTask.FromException<ImmutableList<JobDto>>(new NotImplementedException());
    }
}
