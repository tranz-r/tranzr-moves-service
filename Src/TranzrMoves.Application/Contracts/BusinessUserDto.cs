using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public sealed class BusinessUserDto
{
    public Guid BusinessUserId { get; init; }
    public Guid UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public BusinessUserRole Role { get; init; }
    public BusinessUserStatus Status { get; init; }
}

public sealed class InviteBusinessUserResponse
{
    public Guid BusinessUserId { get; init; }
    public BusinessUserStatus Status { get; init; }
    public Instant? ExpiresAtUtc { get; init; }
}

/// <summary>Result of a revoke/resend invitation action.</summary>
public sealed class InvitationActionResponse
{
    public BusinessUserStatus Status { get; init; }
    public Instant? ExpiresAtUtc { get; init; }
}

/// <summary>Request body for changing a business user's role.</summary>
public sealed class ChangeRoleRequest
{
    public BusinessUserRole Role { get; init; }
}

/// <summary>Result of an ownership transfer: the caller's new role and the target's new role.</summary>
public sealed class TransferOwnershipResponse
{
    public BusinessUserRole PreviousOwnerRole { get; init; }
    public BusinessUserRole NewOwnerRole { get; init; }
}

/// <summary>
/// A pending team invitation (a <c>BusinessUser</c> with <c>Status = Invited</c>), surfaced
/// on the Team page's "Pending invitations" table.
/// </summary>
public sealed class InvitationDto
{
    public Guid BusinessUserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public BusinessUserRole Role { get; init; }
    public BusinessUserStatus Status { get; init; }
    public string? InvitedByName { get; init; }
    public Instant SentAtUtc { get; init; }
    public Instant? ExpiresAtUtc { get; init; }
    public bool IsExpired { get; init; }
}

/// <summary>
/// The authenticated Business User's runtime context, resolved from the Supabase JWT.
/// Returned by the auth context and accept-invitation endpoints.
/// </summary>
public sealed class AuthContextDto
{
    public Guid UserId { get; init; }
    public Guid BusinessUserId { get; init; }
    public Guid BusinessAccountId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public BusinessUserRole Role { get; init; }
}
