using Microsoft.EntityFrameworkCore;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class BusinessUserRepository(TranzrMovesDbContext dbContext) : IBusinessUserRepository
{
    public async Task<BusinessUser?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Set<BusinessUser>()
            .AsNoTracking()
            .Include(x => x.BusinessAccount)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<BusinessUser?> GetBySupabaseIdAsync(Guid supabaseId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<UserV2>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SupabaseId == supabaseId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return await GetByUserIdAsync(user.Id, cancellationToken);
    }
}
