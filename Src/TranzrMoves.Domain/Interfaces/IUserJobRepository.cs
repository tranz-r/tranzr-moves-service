using System.Collections.Immutable;
using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IUserJobRepository
{
    Task<ErrorOr<CustomerJob>> AddUserJobAsync(CustomerJob customerJob,
        CancellationToken cancellationToken);

    Task<CustomerJob?> GetUserJobAsync(Guid userJobId, CancellationToken cancellationToken);
    Task<ImmutableList<CustomerJob>> GetUserJobsAsync(Guid userJobId, CancellationToken cancellationToken);

    Task<ErrorOr<CustomerJob>> UpdateUserJobAsync(CustomerJob customerJob,
        CancellationToken cancellationToken);

    Task DeleteUserJobAsync(CustomerJob customerJob, CancellationToken cancellationToken);
}