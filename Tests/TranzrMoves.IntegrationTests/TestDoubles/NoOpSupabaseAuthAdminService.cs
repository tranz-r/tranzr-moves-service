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

    public Task<ErrorOr<Success>> ResendInvitationAsync(
        SupabaseInviteUserRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<Success>>(Result.Success);

    public Task<ErrorOr<Success>> UpdateUserNameAsync(
        Guid supabaseId,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<Success>>(Result.Success);
}
