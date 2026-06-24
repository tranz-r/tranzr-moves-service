using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IBusinessUserRepository
{
    Task<BusinessUser?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<BusinessUser?> GetBySupabaseIdAsync(Guid supabaseId, CancellationToken cancellationToken);
}
