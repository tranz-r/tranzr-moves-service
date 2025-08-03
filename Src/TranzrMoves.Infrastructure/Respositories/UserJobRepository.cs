using System.Collections.Immutable;
using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class UserJobRepository(TranzrMovesDbContext dbContext, ILogger<UserJobRepository> logger) : IUserJobRepository
{
    public async Task<ErrorOr<UserJob>> AddUserJobAsync(UserJob userJob,
        CancellationToken cancellationToken)
    {
        dbContext.Set<UserJob>().Add(userJob);

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

        return userJob;
    }

    public async Task<UserJob?> GetUserJobAsync(Guid userJobId, CancellationToken cancellationToken)
        => await dbContext.Set<UserJob>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userJobId, cancellationToken);

    public async Task<ImmutableList<UserJob>> GetUserJobsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userJob = await dbContext.Set<UserJob>().AsNoTracking()
            .Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        
        return userJob.ToImmutableList();
    }


    public async Task<ErrorOr<UserJob>> UpdateUserJobAsync(UserJob userJob,
        CancellationToken cancellationToken)
    {
        dbContext.Set<UserJob>().Update(userJob);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating UserJob with UserJobId {UserJobId}",
                userJob.Id);
            return Error.Conflict();
        }

        return userJob;
    }

    public async Task DeleteUserJobAsync(UserJob userJob, CancellationToken cancellationToken)
        => await dbContext.Set<UserJob>()
            .Where(ac => ac.Id == userJob.Id)
            .ExecuteDeleteAsync(cancellationToken);
}