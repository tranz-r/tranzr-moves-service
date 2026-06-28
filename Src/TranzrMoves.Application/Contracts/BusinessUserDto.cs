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
