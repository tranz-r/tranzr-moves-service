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

    /// <summary>
    /// Re-sends a Supabase invitation email for an already-invited user, issuing a fresh
    /// invite link. Unlike <see cref="InviteUserByEmailAsync"/>, an "already exists" response
    /// is treated as success because the invitee is expected to exist.
    /// </summary>
    Task<ErrorOr<Success>> ResendInvitationAsync(
        SupabaseInviteUserRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Syncs a user's name into Supabase <c>user_metadata</c> using the uniform
    /// <c>first_name</c>/<c>last_name</c> keys shared with the invite/create flows, so all GoTrue
    /// email templates can render the recipient via <c>{{ .Data.first_name }}</c>. Existing
    /// metadata keys are preserved (GoTrue merges the supplied keys). Used to capture the owner's
    /// name during business account creation, where the auth account is created passwordlessly.
    /// </summary>
    Task<ErrorOr<Success>> UpdateUserNameAsync(
        Guid supabaseId,
        string? firstName,
        string? lastName,
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
