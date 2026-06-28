using ErrorOr;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.IntegrationTests.TestDoubles;

internal sealed class NoOpSupabaseAuthAdminService : ISupabaseAuthAdminService
{
    public Task<ErrorOr<SupabaseAuthUser>> CreateUserAsync(
        SupabaseAuthUserCreateRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<SupabaseAuthUser>>(
            new SupabaseAuthUser(Guid.NewGuid(), request.Email));

    public Task<ErrorOr<Success>> InviteUserByEmailAsync(
        SupabaseInviteUserRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<Success>>(Result.Success);
}
