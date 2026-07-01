using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class BusinessUser : IAuditable, ITenantOwned
{
    public Guid Id { get; set; }
    public Guid BusinessAccountId { get; set; }
    public Guid UserId { get; set; }
    public BusinessUserRole Role { get; set; }
    public BusinessUserStatus Status { get; set; } = BusinessUserStatus.Active;

    /// <summary>The BusinessUser who created this membership. Null for the Owner created during account registration.</summary>
    public Guid? CreatedByBusinessUserId { get; set; }

    /// <summary>The BusinessUser who last updated this membership's role (role change or ownership transfer).</summary>
    public Guid? UpdatedByBusinessUserId { get; set; }

    /// <summary>
    /// When a pending invitation (Status = Invited) expires. Matches the Supabase invite-link
    /// lifetime (24h). Null once accepted or for memberships not created via invitation.
    /// </summary>
    public Instant? InvitationExpiresAt { get; set; }

    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";

    public BusinessAccount? BusinessAccount { get; set; }
    public UserV2? User { get; set; }
}
