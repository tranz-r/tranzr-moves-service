using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IUserRepository
{
    Task<ErrorOr<User>> AddUserAsync(User user,
        CancellationToken cancellationToken);

    Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetUserByEmailAsync(string? emailAddress, CancellationToken cancellationToken);

    Task<ErrorOr<User>> UpdateUserAsync(User user,
        CancellationToken cancellationToken);

    Task DeleteUserAsync(User user, CancellationToken cancellationToken);
}