using System.Collections.Immutable;
using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IUserJobRepository
{
    Task<ErrorOr<UserJob>> AddUserJobAsync(UserJob userJob,
        CancellationToken cancellationToken);

    Task<UserJob?> GetUserJobAsync(Guid userJobId, CancellationToken cancellationToken);
    Task<ImmutableList<UserJob>> GetUserJobsAsync(Guid userJobId, CancellationToken cancellationToken);

    Task<ErrorOr<UserJob>> UpdateUserJobAsync(UserJob userJob,
        CancellationToken cancellationToken);

    Task DeleteUserJobAsync(UserJob userJob, CancellationToken cancellationToken);
}