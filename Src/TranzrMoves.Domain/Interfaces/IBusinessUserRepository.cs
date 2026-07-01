using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IBusinessUserRepository
{
    Task<BusinessUser?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<BusinessUser?> GetBySupabaseIdAsync(Guid supabaseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BusinessUser>> GetByBusinessAccountIdAsync(
        Guid businessAccountId,
        CancellationToken cancellationToken);

    Task<BusinessUser?> GetByIdAsync(Guid businessUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a membership by UserId ignoring the tenant query filter. Used for the
    /// cross-tenant BR-001 check (a user may only belong to one Business Account).
    /// </summary>
    Task<BusinessUser?> GetByUserIdGlobalAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically persists an invited member: the (new or existing) UserV2 and the
    /// BusinessUser membership in a single transaction.
    /// </summary>
    Task<ErrorOr<BusinessUser>> InviteAsync(
        UserV2 user,
        bool userIsNew,
        BusinessUser businessUser,
        CancellationToken cancellationToken);

    Task<ErrorOr<BusinessUser>> UpdateStatusAsync(
        Guid businessUserId,
        BusinessUserStatus status,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates an invitation's role, status and expiry in one operation. Used to resend a
    /// pending/expired invitation (resetting expiry) and to re-issue a previously revoked one.
    /// Tenant-scoped: only resolves memberships in the caller's business account.
    /// </summary>
    Task<ErrorOr<BusinessUser>> UpdateInvitationAsync(
        Guid businessUserId,
        BusinessUserRole role,
        BusinessUserStatus status,
        Instant? expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically completes an invitation: links the invited UserV2 to its Supabase identity
    /// and transitions the membership from Invited to Active in a single transaction.
    /// </summary>
    Task<ErrorOr<BusinessUser>> AcceptInvitationAsync(
        Guid userId,
        Guid supabaseId,
        Guid businessUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Changes a membership's role and records an audit row in a single transaction.
    /// Tenant-scoped: only resolves memberships in the caller's business account.
    /// </summary>
    Task<ErrorOr<BusinessUser>> ChangeRoleAsync(
        Guid businessUserId,
        BusinessUserRole newRole,
        Guid changedByBusinessUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Transfers ownership in a single transaction: demotes the current Owner to Admin,
    /// promotes the target to Owner, and writes two audit rows. Tenant-scoped.
    /// </summary>
    Task<ErrorOr<BusinessUser>> TransferOwnershipAsync(
        Guid currentOwnerBusinessUserId,
        Guid targetBusinessUserId,
        CancellationToken cancellationToken);
}
