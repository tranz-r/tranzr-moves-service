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
    public async Task<ErrorOr<CustomerJob>> AddUserJobAsync(CustomerJob customerJob,
        CancellationToken cancellationToken)
    {
        dbContext.Set<CustomerJob>().Add(customerJob);

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

        return customerJob;
    }

    public async Task<CustomerJob?> GetUserJobAsync(Guid userJobId, CancellationToken cancellationToken)
        => await dbContext.Set<CustomerJob>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userJobId, cancellationToken);

    public async Task<ImmutableList<CustomerJob>> GetUserJobsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userJob = await dbContext.Set<CustomerJob>().AsNoTracking()
            .Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        
        return userJob.ToImmutableList();
    }

    public async Task<ImmutableList<Job>> GetJobsForCustomerAsync(Guid userId, IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<CustomerJob>().AsNoTracking()
            .Where(cj => cj.UserId == userId)
            .Select(cj => cj.Job)
            .AsQueryable();

        if (statuses is not null && statuses.Any())
        {
            query = query.Where(j => statuses.Contains(j.PaymentStatus));
        }

        var jobs = await query.ToListAsync(cancellationToken);
        return jobs.ToImmutableList();
    }


    public async Task<ErrorOr<CustomerJob>> UpdateUserJobAsync(CustomerJob customerJob,
        CancellationToken cancellationToken)
    {
        dbContext.Set<CustomerJob>().Update(customerJob);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating UserJob with UserJobId {UserJobId}",
                customerJob.Id);
            return Error.Conflict();
        }

        return customerJob;
    }

    public async Task DeleteUserJobAsync(CustomerJob customerJob, CancellationToken cancellationToken)
        => await dbContext.Set<CustomerJob>()
            .Where(ac => ac.Id == customerJob.Id)
            .ExecuteDeleteAsync(cancellationToken);
}