using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class QuoteRepository(TranzrMovesDbContext db, ILogger<QuoteRepository> logger) : IQuoteRepository
{
    public async Task CreateIfMissingAsync(string guestId, CancellationToken ct = default)
    {
        var session = await db.Set<QuoteSession>()
            .FirstOrDefaultAsync(s => s.SessionId == guestId, ct);

        if (session is null)
        {
            session = new QuoteSession
            {
                SessionId = guestId,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(60),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            db.Set<QuoteSession>().Add(session);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<Quote?> GetQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default)
    {
        return await db.Set<Quote>()
            .Where(q => q.SessionId == guestId && q.Type == quoteType)
            .Include(q => q.InventoryItems)
            .FirstOrDefaultAsync(ct);
    }
    
    public async Task<Quote?> GetQuoteAsync(Guid quoteId, CancellationToken ct = default)
    {
        return await db.Set<Quote>()
            .Include(q => q.InventoryItems)
            .Where(q => q.Id == quoteId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Quote?> GetOrCreateQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default)
    {
        var quote = await GetQuoteAsync(guestId, quoteType, ct);

        if (quote is not null && quote.PaymentStatus == PaymentStatus.Pending) return quote;
        
        // Create new quote
        quote = new Quote
        {
            SessionId = guestId,
            Type = quoteType,
            QuoteReference = GenerateQuoteReference(),
            VanType = VanType.largeVan, // Default
            DriverCount = 1, // Default
        };

        db.Set<Quote>().Add(quote);
        await db.SaveChangesAsync(ct);

        return quote;
    }

    public async Task<ErrorOr<Quote>> UpdateQuoteAsync(Quote quote, CancellationToken ct = default)
    {
        try
        {
            // 🔍 TEST: Log Version before saving to see what EF Core will work with
            logger.LogInformation("🔍 REPOSITORY TEST - Before save: Quote {QuoteId} Version = {Version}",
                quote.Id, quote.Version);

            db.Set<Quote>().Update(quote);
            await db.SaveChangesAsync(ct);

            // 🔍 TEST: Log Version after saving to see if EF Core incremented it
            logger.LogInformation("🔍 REPOSITORY TEST - After save: Quote {QuoteId} Version = {Version}",
                quote.Id, quote.Version);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex,
                "🔍 CONCURRENCY EXCEPTION: EF Core detected version conflict! Quote {QuoteId}, QuoteReference: {QuoteReference}",
                quote.Id, quote.QuoteReference);
            return Error.Conflict();
        }


        return quote;
    }

    public async Task<bool> DeleteQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default)
    {
        var quote = await db.Set<Quote>()
            .FirstOrDefaultAsync(q => q.SessionId == guestId && q.Type == quoteType, ct);

        if (quote is null) return false;

        db.Set<Quote>().Remove(quote);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<Quote?> GetQuoteByReferenceAsync(string quoteReference, string paymentIntentId, CancellationToken cancellationToken = default)
    {
        return db.Set<Quote>()
            .Include(x => x.InventoryItems)
            .FirstOrDefaultAsync(q => q.QuoteReference == quoteReference && q.PaymentIntentId == paymentIntentId , cancellationToken);
    }

    private static string GenerateQuoteReference()
    {
        // Simple quote reference generation
        return $"TRZ-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
    }
}