using System.Collections.Immutable;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class DriverJobRepository(TranzrMovesDbContext dbContext, ILogger<DriverJobRepository> logger) : IDriverJobRepository
{
    public async Task<ErrorOr<DriverJob>> AddDriverJobAsync(DriverJob driverJob, CancellationToken cancellationToken)
    {
        dbContext.Set<DriverJob>().Add(driverJob);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return driverJob;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to add DriverJob for Driver {DriverId} and Job {JobId}", driverJob.UserId, driverJob.JobId);
            return Error.Conflict();
        }
    }

    public async Task<DriverJob?> GetDriverJobAsync(Guid driverJobId, CancellationToken cancellationToken)
        => await dbContext.Set<DriverJob>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == driverJobId, cancellationToken);

    public async Task<DriverJob?> GetDriverJobAsync(Guid driverId, Guid jobId, CancellationToken cancellationToken)
        => await dbContext.Set<DriverJob>().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == driverId && x.JobId == jobId, cancellationToken);

    public async Task<ImmutableList<Job>> GetJobsForDriverAsync(Guid driverId, IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<DriverJob>().AsNoTracking()
            .Where(dj => dj.UserId == driverId)
            .Select(dj => dj.Job)
            .AsQueryable();

        if (statuses is not null && statuses.Any())
        {
            query = query.Where(j => statuses.Contains(j.PaymentStatus));
        }

        var jobs = await query.ToListAsync(cancellationToken);
        return jobs.ToImmutableList();
    }

    public async Task DeleteDriverJobAsync(DriverJob driverJob, CancellationToken cancellationToken)
        => await dbContext.Set<DriverJob>().Where(x => x.Id == driverJob.Id).ExecuteDeleteAsync(cancellationToken);
}
