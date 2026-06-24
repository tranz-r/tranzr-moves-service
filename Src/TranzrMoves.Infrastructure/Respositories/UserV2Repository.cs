using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class UserV2Repository(TranzrMovesDbContext dbContext, ILogger<UserV2Repository> logger) : IUserV2Repository
{
    public async Task<ErrorOr<UserV2>> AddUserAsync(UserV2 user,
        CancellationToken cancellationToken)
    {
        dbContext.Set<UserV2>().Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (CannotInsertNullException e)
        {
            logger.LogError("Cannot insert null value for {property}", e.Source);
            return Error.Custom(
                type: (int)CustomErrorType.BadRequest,
                code: "Null.Value",
                description: "Cannot insert null value");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating User with UserId {UserId}",
                user.Id);
            return Error.Conflict();
        }
        catch (UniqueConstraintException e)
        {
            logger.LogError("Unique constraint {constraintName} violated. Duplicate value for {constraintProperty}",
                e.ConstraintName, e.ConstraintProperties[0]);
            return Error.Conflict();
        }

        return user;
    }

    public async Task<UserV2?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Set<UserV2>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public async Task<UserV2?> GetUserByEmailAsync(string? emailAddress, CancellationToken cancellationToken)
        => await dbContext.Set<UserV2>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == emailAddress, cancellationToken);

    public async Task<UserV2?> GetUserBySupabaseIdAsync(Guid supabaseId, CancellationToken cancellationToken)
        => await dbContext.Set<UserV2>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.SupabaseId == supabaseId, cancellationToken);

    public async Task<ErrorOr<UserV2>> UpdateUserAsync(UserV2 user,
        CancellationToken cancellationToken)
    {
        dbContext.Set<UserV2>().Update(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating User with UserId {UserId}",
                user.Id);
            return Error.Conflict();
        }

        return user;
    }

    public async Task DeleteUserAsync(UserV2 user, CancellationToken cancellationToken)
        => await dbContext.Set<UserV2>()
            .Where(ac => ac.Id == user.Id)
            .ExecuteDeleteAsync(cancellationToken);
}
