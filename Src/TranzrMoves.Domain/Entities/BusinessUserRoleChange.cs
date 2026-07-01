using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

/// <summary>
/// Immutable audit record of a role change or ownership transfer within a Business Account.
/// One row is written per affected membership (an ownership transfer writes two: the demoted
/// Owner and the promoted target).
/// </summary>
public class BusinessUserRoleChange : IAuditable, ITenantOwned
{
    public Guid Id { get; set; }
    public Guid BusinessAccountId { get; set; }

    /// <summary>The membership whose role changed.</summary>
    public Guid TargetBusinessUserId { get; set; }

    /// <summary>The membership that performed the change.</summary>
    public Guid ChangedByBusinessUserId { get; set; }

    public BusinessUserRole FromRole { get; set; }
    public BusinessUserRole ToRole { get; set; }
    public RoleChangeType ChangeType { get; set; }

    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}
