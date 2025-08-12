using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IJobRepository
{
    Task<ErrorOr<Job>> AddJobAsync(Job job, CancellationToken cancellationToken);
    Task<Job?> GetJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<Job?> GetJobByQuoteIdAsync(string quoteId, CancellationToken cancellationToken);
    Task<ErrorOr<Job>> UpdateJobAsync(Job job, CancellationToken cancellationToken);

    Task DeleteJobAsync(Job job, CancellationToken cancellationToken);
}