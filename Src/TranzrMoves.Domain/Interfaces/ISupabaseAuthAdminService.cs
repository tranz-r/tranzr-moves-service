using ErrorOr;

namespace TranzrMoves.Domain.Interfaces;

public interface ISupabaseAuthAdminService
{
    Task<ErrorOr<SupabaseAuthUser>> CreateUserAsync(
        SupabaseAuthUserCreateRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a Supabase invitation email (creating the auth account). Seeds
    /// <c>user_metadata</c> with first/last name, role and business account id.
    /// </summary>
    Task<ErrorOr<Success>> InviteUserByEmailAsync(
        SupabaseInviteUserRequest request,
        CancellationToken cancellationToken);
}

public sealed record SupabaseAuthUserCreateRequest(
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber);

public sealed record SupabaseInviteUserRequest(
    string Email,
    string? FirstName,
    string? LastName,
    string Role,
    Guid BusinessAccountId);

public sealed record SupabaseAuthUser(Guid Id, string Email);
