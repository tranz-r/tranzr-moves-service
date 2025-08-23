using System.Collections.Immutable;
using EntityFramework.Exceptions.Common;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class UserQuoteRepository(TranzrMovesDbContext dbContext, ILogger<UserQuoteRepository> logger) : IUserQuoteRepository
{
    public async Task<ErrorOr<CustomerQuote>> AddUserJobAsync(CustomerQuote customerQuote,
        CancellationToken cancellationToken)
    {
        dbContext.Set<CustomerQuote>().Add(customerQuote);

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

        return customerQuote;
    }
    
    public async Task<ErrorOr<List<CustomerQuote>>> AddUserQuotesAsync(List<CustomerQuote> customerQuotes,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<CustomerQuote>().AddRangeAsync(customerQuotes, cancellationToken);

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

        return customerQuotes;
    }

    public async Task<CustomerQuote?> GetUserJobAsync(Guid userJobId, CancellationToken cancellationToken)
        => await dbContext.Set<CustomerQuote>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userJobId, cancellationToken);

    public async Task<ImmutableList<CustomerQuote>> GetUserJobsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userJob = await dbContext.Set<CustomerQuote>().AsNoTracking()
            .Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        
        return userJob.ToImmutableList();
    }

    // public async Task<ImmutableList<Quote>> GetJobsForCustomerAsync(Guid userId, IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken)
    // {
    //     var query = dbContext.Set<CustomerJob>().AsNoTracking()
    //         .Where(cj => cj.UserId == userId)
    //         .Select(cj => cj.Quote)
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


    public async Task<ErrorOr<CustomerQuote>> UpdateUserJobAsync(CustomerQuote customerQuote,
        CancellationToken cancellationToken)
    {
        dbContext.Set<CustomerQuote>().Update(customerQuote);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating UserJob with UserJobId {UserJobId}",
                customerQuote.Id);
            return Error.Conflict();
        }

        return customerQuote;
    }

    public async Task DeleteUserJobAsync(CustomerQuote customerQuote, CancellationToken cancellationToken)
        => await dbContext.Set<CustomerQuote>()
            .Where(ac => ac.Id == customerQuote.Id)
            .ExecuteDeleteAsync(cancellationToken);
}