using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class UserRepository(TranzrMovesDbContext dbContext, ILogger<UserRepository> logger) : IUserRepository
{
    public async Task<ErrorOr<User>> AddUserAsync(User user,
        CancellationToken cancellationToken)
    {
        dbContext.Set<User>().Add(user);

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
        catch (UniqueConstraintException e)
        {
            logger.LogError("Unique constraint {constraintName} violated. Duplicate value for {constraintProperty}",
                e.ConstraintName, e.ConstraintProperties[0]);
            return Error.Conflict();
        }

        return user;
    }

    public async Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Set<User>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);    
    
    public async Task<User?> GetUserByEmailAsync(string? emailAddress, CancellationToken cancellationToken)
        => await dbContext.Set<User>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == emailAddress, cancellationToken);


    public async Task<ErrorOr<User>> UpdateUserAsync(User user,
        CancellationToken cancellationToken)
    {
        dbContext.Set<User>().Update(user);

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

    public async Task DeleteUserAsync(User user, CancellationToken cancellationToken)
        => await dbContext.Set<User>()
            .Where(ac => ac.Id == user.Id)
            .ExecuteDeleteAsync(cancellationToken);
}