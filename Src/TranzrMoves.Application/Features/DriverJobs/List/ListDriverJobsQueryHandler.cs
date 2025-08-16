using System.Collections.Immutable;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.DriverJobs.List;

public class ListDriverJobsQueryHandler(
    IDriverJobRepository driverJobRepository
) : IQueryHandler<ListDriverJobsQuery, ImmutableList<JobDto>>
{
    public async ValueTask<ImmutableList<JobDto>> Handle(ListDriverJobsQuery query, CancellationToken cancellationToken)
    {
        var jobs = await driverJobRepository.GetJobsForDriverAsync(query.DriverId, query.Statuses, cancellationToken);
        var mapper = new JobMapper();
        return jobs.Select(mapper.MapJobToDto).ToImmutableList();
    }
}
