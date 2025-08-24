using Microsoft.EntityFrameworkCore;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace TranzrMoves.Infrastructure.Respositories;

public class QuoteRepository(TranzrMovesDbContext db) : IQuoteRepository
{
    public async Task CreateIfMissingAsync(string guestId, CancellationToken ct = default)
    {
        var existing = await db.Set<QuoteSession>().FindAsync([guestId], ct);
        if (existing is null)
        {
            var session = new QuoteSession
            {
                SessionId = guestId,
                ETag = ComputeWeakETag("{}"),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(60)
            };
            db.Set<QuoteSession>().Add(session);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<QuoteSession?> GetSessionAsync(string guestId, CancellationToken ct = default)
    {
        return await db.Set<QuoteSession>()
            .Include(s => s.Quotes)
            .FirstOrDefaultAsync(s => s.SessionId == guestId, ct);
    }

    public async Task<List<Quote>> GetQuoteContextStateAsync(string guestId, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(guestId, ct);
        if (session is null) return [];

        // Convert QuoteSession to QuoteContextState
        // var state = new QuoteContextDto
        // {
        //     ActiveQuoteType = session.Quotes.FirstOrDefault()?.Type.ToString().ToLower(),
        //     Quotes = session.Quotes.ToDictionary(
        //         q => q.Type.ToString().ToLower(),
        //         q => new QuoteDataDto
        //         {
        //             VanType = q.VanType,
        //             DriverCount = q.DriverCount,
        //             Origin = q.Origin != null ? new AddressDto
        //             {
        //                 Line1 = q.Origin.Line1,
        //                 Line2 = q.Origin.Line2,
        //                 City = q.Origin.City,
        //                 PostCode = q.Origin.PostCode,
        //                 Country = q.Origin.Country
        //             } : null,
        //             Destination = q.Destination != null ? new AddressDto
        //             {
        //                 Line1 = q.Destination.Line1,
        //                 Line2 = q.Destination.Line2,
        //                 City = q.Destination.City,
        //                 PostCode = q.Destination.PostCode,
        //                 Country = q.Destination.Country
        //             } : null,
        //             DistanceMiles = q.DistanceMiles,
        //             NumberOfItemsToDismantle = q.NumberOfItemsToDismantle,
        //             NumberOfItemsToAssemble = q.NumberOfItemsToAssemble,
        //             Schedule = new ScheduleDto
        //             {
        //                 DateISO = q.CollectionDate?.ToString("O"),
        //                 DeliveryDateISO = q.DeliveryDate?.ToString("O"),
        //                 Hours = q.Hours,
        //                 FlexibleTime = q.FlexibleTime,
        //                 TimeSlot = q.TimeSlot
        //             },
        //             Pricing = new Pricing
        //             {
        //                 PricingTier = q.PricingTier?.ToString(),
        //                 TotalCost = q.TotalCost
        //             },
        //             Items = q.InventoryItems.Select(x => new InventoryItemDto
        //             {
        //                 Name = x.Name,
        //                 Quantity = x.Quantity,
        //                 Depth =  x.Depth,
        //                 Description = x.Description,
        //                 Height = x.Height,
        //                 Width =  x.Width,
        //                 Id = x.Id
        //             }).ToList(),
        //             Payment = new PaymentDto
        //             {
        //                 Status = q.PaymentStatus?.ToString()
        //             },
        //             Customer = q.Customer != null ? new Customer
        //             {
        //                 FullName = q.Customer.FullName,
        //                 Email = q.Customer.Email,
        //                 Phone = q.Customer.PhoneNumber,
        //                 BillingAddress = q.Customer.BillingAddress != null ? new Address
        //                 {
        //                     Line1 = q.Customer.BillingAddress.Line1,
        //                     Line2 = q.Customer.BillingAddress.Line2,
        //                     City = q.Customer.BillingAddress.City,
        //                     Postcode = q.Customer.BillingAddress.PostCode,
        //                     Country = q.Customer.BillingAddress.Country
        //                 } : null
        //             } : null
        //         }
        //     ),
        //     Metadata = new QuoteContextMetadata
        //     {
        //         LastActiveQuoteType = session.Quotes.FirstOrDefault()?.Type.ToString().ToLower(),
        //         LastActivity = session.ModifiedAt.ToString("O"),
        //         Version = "1.0.0"
        //     }
        // };

        return session.Quotes;
    }

    public async Task<List<Quote>> SaveQuoteContextStateAsync(string guestId, Dictionary<QuoteType, Quote> quotes, string? providedEtag, CancellationToken ct = default)
    {
        var session = await GetSessionAsync(guestId, ct);
        if (session is null) return [];

        // Check ETag for concurrency
        if (!string.IsNullOrEmpty(providedEtag) && session.ETag != providedEtag)
        {
            return []; // ETag mismatch
        }

        // Update quotes
        session.Quotes.Clear();
        
        var quotesToStore = quotes.Values.ToList();
        session.Quotes.AddRange(quotesToStore);
        // Update ETag and metadata
        session.ETag = ComputeWeakETag(quotes);
        session.ModifiedAt = DateTime.UtcNow;
        session.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(60);

        await db.SaveChangesAsync(ct);
        return quotesToStore;
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
        // Try to get existing quote
        var existingQuote = await GetQuoteAsync(guestId, quoteType, ct);
        if (existingQuote != null)
        {
            return existingQuote;
        }

        // Create new quote with generated reference
        var newQuote = new Quote
        {
            Id = Guid.NewGuid(),
            SessionId = guestId,
            Type = quoteType,
            QuoteReference = Application.Helpers.BookingNumberGenerator.Generate(),
            // All other properties remain null/empty to be filled in later
            VanType = default,
            DriverCount = 1,
            DistanceMiles = null,
            NumberOfItemsToDismantle = 0,
            NumberOfItemsToAssemble = 0,
            Origin = null,
            Destination = null,
            CollectionDate = null,
            DeliveryDate = null,
            Hours = null,
            FlexibleTime = null,
            TimeSlot = null,
            PricingTier = null,
            TotalCost = null,
            PaymentStatus = null,
            PaymentType = default,
            DepositAmount = null,
            ReceiptUrl = null,
            InventoryItems = new List<InventoryItem>(),
            CustomerQuotes = null,
            DriverQuotes = null
        };

        // Add to database
        db.Set<Quote>().Add(newQuote);
        await db.SaveChangesAsync(ct);

        return newQuote;
    }

    public async Task<List<Quote>> GetQuotesForSessionAsync(string guestId, CancellationToken ct = default)
    {
        return await db.Set<Quote>()
            .Where(q => q.SessionId == guestId)
            .Include(q => q.InventoryItems)
            .ToListAsync(ct);
    }

    public async Task<Quote?> UpsertQuoteAsync(string guestId, Quote quote, string? providedEtag, CancellationToken ct = default)
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

        //TODO: Do something here
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

    // public async Task<bool> UpdateSharedDataAsync(string guestId, SharedData sharedData, string? providedEtag, CancellationToken ct = default)
    // {
    //     var session = await GetSessionAsync(guestId, ct);
    //     if (session is null) return false;
    //
    //     // Check ETag for concurrency
    //     if (!string.IsNullOrEmpty(providedEtag) && session.ETag != providedEtag)
    //     {
    //         return false; // ETag mismatch
    //     }
    //
    //     // Update shared data (same logic as in SaveQuoteContextStateAsync)
    //     if (sharedData != null)
    //     {
    //         // Customer data is now stored per quote, not in session
    //     }
    //
    //     // Update ETag and metadata
    //     session.ETag = ComputeWeakETag(sharedData);
    //     session.ModifiedAt = DateTime.UtcNow;
    //
    //     await db.SaveChangesAsync(ct);
    //     return true;
    // }

    private static string ComputeWeakETag(object obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return $"W/\"{Convert.ToBase64String(hash)}\"";
    }
}


