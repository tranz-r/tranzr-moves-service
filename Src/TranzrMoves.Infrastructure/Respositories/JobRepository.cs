using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class JobRepository(TranzrMovesDbContext dbContext, ILogger<JobRepository> logger) : IJobRepository
{
    public async Task<ErrorOr<Job>> AddJobAsync(Job job,
        CancellationToken cancellationToken)
    {
        dbContext.Set<Job>().Add(job);

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
                description: e.Message);
        }
        catch (UniqueConstraintException e)
        {
            logger.LogError("Unique constraint {constraintName} violated. Duplicate value for {constraintProperty}",
                e.ConstraintName, e.ConstraintProperties[0]);
            return Error.Conflict();
        }

        return job;
    }

    public async Task<Job?> GetJobAsync(Guid jobId, CancellationToken cancellationToken)
        => await dbContext.Set<Job>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

    public async Task<Job?> GetJobByQuoteIdAsync(string quoteId, CancellationToken cancellationToken)
        => await dbContext.Set<Job>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.QuoteId == quoteId, cancellationToken);


    public async Task<ErrorOr<Job>> UpdateJobAsync(Job job,
        CancellationToken cancellationToken)
    {
        dbContext.Set<Job>().Update(job);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating Job with JobId {JobId}",
                job.Id);
            return Error.Conflict();
        }

        return job;
    }

    public async Task DeleteJobAsync(Job job, CancellationToken cancellationToken)
        => await dbContext.Set<Job>()
            .Where(ac => ac.Id == job.Id)
            .ExecuteDeleteAsync(cancellationToken);
}