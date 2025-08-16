using System.Collections.Immutable;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Jobs.ListAll;

public sealed class ListAllJobsQueryHandler(
    IJobRepository jobRepository,
    ILogger<ListAllJobsQueryHandler> logger) : IQueryHandler<ListAllJobsQuery, ImmutableList<JobDto>>
{
    public async ValueTask<ImmutableList<JobDto>> Handle(ListAllJobsQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Listing all jobs");
        var jobs = await jobRepository.GetJobsAsync(cancellationToken);
        var mapper = new JobMapper();
        return jobs.Select(mapper.MapJobToDto).ToImmutableList();
    }
}
