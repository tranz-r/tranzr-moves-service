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
    public async Task<ErrorOr<CustomerQuote>> AddUserQuoteAsync(CustomerQuote customerQuote,
        CancellationToken cancellationToken)
    {
        //Check if this customer quote already exists for this user and quote
        var existing = await dbContext.Set<CustomerQuote>()
            .AsNoTracking()
            .FirstOrDefaultAsync(cc => cc.UserId == customerQuote.UserId 
                                       && cc.QuoteId == customerQuote.QuoteId, cancellationToken);
        
        if (existing is not null)
        {
            return Error.Conflict("CustomerQuote.AlreadyExists", "A customer quote already exists for this user and quote");
        }
        
        
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

    public async Task<CustomerQuote?> GetUserQuoteAsync(Guid userQuoteId, CancellationToken cancellationToken)
        => await dbContext.Set<CustomerQuote>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userQuoteId, cancellationToken);

    public async Task<ImmutableList<CustomerQuote>> GetUserQuotesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userQuotes = await dbContext.Set<CustomerQuote>().AsNoTracking()
            .Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        
        return userQuotes.ToImmutableList();
    }

    public async Task<CustomerQuote?> GetUserQuoteByQuoteIdAsync(Guid quoteId, CancellationToken cancellationToken)
        => await dbContext.Set<CustomerQuote>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.QuoteId == quoteId, cancellationToken);

    public async Task<ErrorOr<CustomerQuote>> UpdateUserQuoteAsync(CustomerQuote customerQuote,
        CancellationToken cancellationToken)
    {
        dbContext.Set<CustomerQuote>().Update(customerQuote);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency exception occurred while updating UserQuote with UserQuoteId {UserQuoteId}",
                customerQuote.Id);
            return Error.Conflict();
        }

        return customerQuote;
    }

    public async Task DeleteUserQuoteAsync(CustomerQuote customerQuote, CancellationToken cancellationToken)
        => await dbContext.Set<CustomerQuote>()
            .Where(ac => ac.Id == customerQuote.Id)
            .ExecuteDeleteAsync(cancellationToken);
}