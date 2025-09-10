using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class DriverQuoteRepository(TranzrMovesDbContext dbContext, ILogger<DriverQuoteRepository> logger) : IDriverQuoteRepository
{
    public async Task<ErrorOr<DriverQuote>> AddDriverQuoteAsync(DriverQuote driverQuote, CancellationToken cancellationToken)
    {
        dbContext.Set<DriverQuote>().Add(driverQuote);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return driverQuote;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to add DriverQuote for Driver {DriverId} and Quote {QuoteId}", driverQuote.UserId, driverQuote.QuoteId);
            return Error.Conflict();
        }
    }

    public async Task<DriverQuote?> GetDriverQuoteAsync(Guid driverQuoteId, CancellationToken cancellationToken)
        => await dbContext.Set<DriverQuote>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == driverQuoteId, cancellationToken);

    public async Task<DriverQuote?> GetDriverQuoteAsync(Guid driverId, Guid jobId, CancellationToken cancellationToken)
        => await dbContext.Set<DriverQuote>().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == driverId && x.QuoteId == jobId, cancellationToken);

    // public async Task<ImmutableList<Quote>> GetQuotesForDriverAsync(Guid driverId, IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken)
    // {
    //     var query = dbContext.Set<DriverQuote>().AsNoTracking()
    //         .Where(dj => dj.UserId == driverId)
    //         .Select(dj => dj.Quote)
    //         .AsQueryable();
    //
    //     if (statuses is not null && statuses.Any())
    //     {
    //         query = query.Where(j => statuses.Contains(j.PaymentStatus));
    //     }
    //
    //     var jobs = await query.ToListAsync(cancellationToken);
    //     return jobs.ToImmutableList();
    // }

    public async Task DeleteDriverQuoteAsync(DriverQuote driverQuote, CancellationToken cancellationToken)
        => await dbContext.Set<DriverQuote>().Where(x => x.Id == driverQuote.Id).ExecuteDeleteAsync(cancellationToken);
}
