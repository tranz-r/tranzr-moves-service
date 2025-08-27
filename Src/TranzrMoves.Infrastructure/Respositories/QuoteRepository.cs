using Microsoft.EntityFrameworkCore;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class QuoteRepository(TranzrMovesDbContext db) : IQuoteRepository
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

    public async Task<Quote?> GetOrCreateQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default)
    {
        var quote = await GetQuoteAsync(guestId, quoteType, ct);
        
        if (quote is null)
        {
            // Create new quote
            quote = new Quote
            {
                SessionId = guestId,
                Type = quoteType,
                QuoteReference = GenerateQuoteReference(),
                VanType = VanType.largeVan, // Default
                DriverCount = 1, // Default
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            
            db.Set<Quote>().Add(quote);
            await db.SaveChangesAsync(ct);
        }
        
        return quote;
    }

    public async Task<Quote?> UpsertQuoteAsync(string guestId, Quote quote, uint? providedVersion, CancellationToken ct = default)
    {
        // Set session ID
        quote.SessionId = guestId;
        
        var existing = await db.Set<Quote>()
            .FirstOrDefaultAsync(q => q.SessionId == guestId && q.Type == quote.Type, ct);
        
        if (existing is null)
        {
            // Create new quote
            quote.Id = Guid.NewGuid();
            quote.CreatedAt = DateTime.UtcNow;
            quote.ModifiedAt = DateTime.UtcNow;
            db.Set<Quote>().Add(quote);
            
            await db.SaveChangesAsync(ct);
            return quote;
        }

        // Check concurrency using Version (xmin)
        if (providedVersion.HasValue && existing.Version != providedVersion.Value)
        {
            // Version mismatch - quote was modified by another request
            return null;
        }
        
        // Update existing quote
        existing.VanType = quote.VanType;
        existing.DriverCount = quote.DriverCount;
        existing.DistanceMiles = quote.DistanceMiles;
        existing.NumberOfItemsToDismantle = quote.NumberOfItemsToDismantle;
        existing.NumberOfItemsToAssemble = quote.NumberOfItemsToAssemble;
        existing.Origin = quote.Origin;
        existing.Destination = quote.Destination;
        existing.CollectionDate = quote.CollectionDate;
        existing.DeliveryDate = quote.DeliveryDate;
        existing.Hours = quote.Hours;
        existing.FlexibleTime = quote.FlexibleTime;
        existing.TimeSlot = quote.TimeSlot;
        existing.PricingTier = quote.PricingTier;
        existing.TotalCost = quote.TotalCost;
        existing.PaymentStatus = quote.PaymentStatus;
        existing.ReceiptUrl = quote.ReceiptUrl;
        existing.ModifiedAt = DateTime.UtcNow;
            
        // Update inventory items
        existing.InventoryItems.Clear();
        foreach (var item in quote.InventoryItems)
        {
            existing.InventoryItems.Add(item);
        }

        db.Set<Quote>().Update(existing);
            
        await db.SaveChangesAsync(ct);
        return existing;
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

    private static string GenerateQuoteReference()
    {
        // Simple quote reference generation
        return $"TRZ-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
    }
}



