using System.Collections.Immutable;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.CustomerJobs.List;

public class ListCustomerJobsQueryHandler(
    IUserQuoteRepository userQuoteRepository
) : IQueryHandler<ListCustomerJobsQuery, ImmutableList<JobDto>>
{
    public async ValueTask<ImmutableList<JobDto>> Handle(ListCustomerJobsQuery query, CancellationToken cancellationToken)
    {
        // var jobs = await userJobRepository.GetJobsForCustomerAsync(query.CustomerId, query.Statuses, cancellationToken);
        // var mapper = new JobMapper();
        // return jobs.Select(mapper.MapJobToDto).ToImmutableList();
        
        throw new NotImplementedException();
    }
}
