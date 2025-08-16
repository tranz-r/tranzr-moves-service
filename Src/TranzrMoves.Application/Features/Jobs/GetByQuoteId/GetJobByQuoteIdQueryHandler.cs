using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Jobs.GetByQuoteId;

public sealed class GetJobByQuoteIdQueryHandler(
    IJobRepository jobRepository,
    ILogger<GetJobByQuoteIdQueryHandler> logger) : IQueryHandler<GetJobByQuoteIdQuery, ErrorOr<JobDto>>
{
    public async ValueTask<ErrorOr<JobDto>> Handle(GetJobByQuoteIdQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.QuoteId))
        {
            return Error.Validation(code: "QuoteId", description: "quoteId is required");
        }
        logger.LogInformation("Fetching job by quoteId {QuoteId}", query.QuoteId);
        var job = await jobRepository.GetJobByQuoteIdAsync(query.QuoteId, cancellationToken);
        if (job is null)
        {
            return Error.NotFound(description: "Job not found");
        }
        var mapper = new JobMapper();
        return mapper.MapJobToDto(job);
    }
}
