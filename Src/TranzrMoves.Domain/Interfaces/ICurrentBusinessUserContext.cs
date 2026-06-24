using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface ICurrentBusinessUserContext
{
    Guid? SupabaseId { get; }
    string? Email { get; }
    Guid? UserId { get; }
    Guid? BusinessAccountId { get; }
    Guid? BusinessUserId { get; }
    BusinessUserRole? Role { get; }
    BusinessUserStatus? Status { get; }
    bool IsAuthenticated { get; }
    Task<BusinessUser?> GetBusinessUserAsync(CancellationToken cancellationToken);
}
