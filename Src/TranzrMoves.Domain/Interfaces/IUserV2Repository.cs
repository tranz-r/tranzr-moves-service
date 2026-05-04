using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IUserV2Repository
{
    Task<ErrorOr<UserV2>> AddUserAsync(UserV2 user,
        CancellationToken cancellationToken);

    Task<UserV2?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserV2?> GetUserByEmailAsync(string? emailAddress, CancellationToken cancellationToken);

    Task<ErrorOr<UserV2>> UpdateUserAsync(UserV2 user,
        CancellationToken cancellationToken);

    Task DeleteUserAsync(UserV2 user, CancellationToken cancellationToken);
}
