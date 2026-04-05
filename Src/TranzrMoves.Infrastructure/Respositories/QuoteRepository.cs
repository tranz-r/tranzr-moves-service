using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Helpers;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class QuoteRepository(TranzrMovesDbContext db, ITimeService timeService, ILogger<QuoteRepository> logger) : IQuoteRepository
{
    private static readonly DateTimeZone Utc = DateTimeZone.Utc;

    public async Task CreateIfMissingAsync(string guestId, CancellationToken ct = default)
    {
        var session = await db.Set<QuoteSession>()
            .FirstOrDefaultAsync(s => s.SessionId == guestId, ct);

        if (session is null)
        {
            var now = timeService.Now();
            session = new QuoteSession
            {
                SessionId = guestId,
                ExpiresUtc = now.Plus(Duration.FromDays(60)),
                CreatedAt = now,
                ModifiedAt = now
            };

            db.Set<QuoteSession>().Add(session);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<Quote?> GetQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default)
    {
        return await db.Set<Quote>()
            .Where(q => q.SessionId == guestId && q.Type == quoteType)
            .Include(x => x.QuoteAdditionalPayments)
            .Include(q => q.InventoryItems)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Quote?> GetQuoteAsync(Guid quoteId, CancellationToken ct = default)
    {
        return await db.Set<Quote>()
            .Include(q => q.InventoryItems)
            .Include(x => x.QuoteAdditionalPayments)
            .Where(q => q.Id == quoteId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Quote?> GetOrCreateQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default)
    {
        var quote = await GetQuoteAsync(guestId, quoteType, ct);

        if (quote is not null && quote.PaymentStatus == PaymentStatus.Pending) return quote;

        var utcToday = timeService.TodayInUtc();
        // EF maps primitive SqlQuery results to a column named "Value" (see EF Core raw SQL docs).
        var sequence = await db.Database
            .SqlQueryRaw<long>(
                $"""SELECT nextval('{Db.SCHEMA}.{Db.Sequences.QuoteReference}') AS "Value" """)
            .SingleAsync(ct);

        quote = new Quote
        {
            SessionId = guestId,
            Type = quoteType,
            QuoteReference = QuoteReferenceHelper.FormatQuoteReference(utcToday, sequence),
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

            // For owned entity collections, we need to handle them specially
            // First, load the existing quote to get the current inventory items
            var existingQuote = await db.Set<Quote>()
                .FirstOrDefaultAsync(q => q.Id == quote.Id, ct);

            if (existingQuote == null)
            {
                logger.LogError("Quote {QuoteId} not found for update", quote.Id);
                return Error.NotFound("Quote.NotFound", $"Quote with ID {quote.Id} not found");
            }

            // 🔍 DEBUG: Log quote data before repository update
            logger.LogInformation("🔍 REPOSITORY DEBUG - Before update: Quote {QuoteId}, TotalCost: {TotalCost}, PaymentStatus: {PaymentStatus}, PaymentIntentId: {PaymentIntentId}",
                existingQuote.Id, existingQuote.TotalCost, existingQuote.PaymentStatus, existingQuote.PaymentIntentId);

            db.Set<Quote>().Update(quote);

            // 🔍 DEBUG: Log quote data after repository update
            logger.LogInformation("🔍 REPOSITORY DEBUG - After update: Quote {QuoteId}, TotalCost: {TotalCost}, PaymentStatus: {PaymentStatus}, PaymentIntentId: {PaymentIntentId}",
                existingQuote.Id, existingQuote.TotalCost, existingQuote.PaymentStatus, existingQuote.PaymentIntentId);

            await db.SaveChangesAsync(ct);

            // 🔍 TEST: Log Version after saving to see if EF Core incremented it
            logger.LogInformation("🔍 REPOSITORY TEST - After save: Quote {QuoteId} Version = {Version}",
                existingQuote.Id, existingQuote.Version);

            return existingQuote;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex,
                "🔍 CONCURRENCY EXCEPTION: EF Core detected version conflict! Quote {QuoteId}, QuoteReference: {QuoteReference}",
                quote.Id, quote.QuoteReference);
            return Error.Conflict();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating quote {QuoteId}", quote.Id);
            return Error.Failure("Quote.UpdateError", "Failed to update quote");
        }
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
            .Include(x => x.QuoteAdditionalPayments)
            .FirstOrDefaultAsync(q => q.QuoteReference == quoteReference && q.PaymentIntentId == paymentIntentId , cancellationToken);
    }

    public Task<Quote?> GetQuoteByReferenceAsync(string quoteReference, CancellationToken cancellationToken = default)
    {
        return db.Set<Quote>()
            .Include(x => x.InventoryItems)
            .Include(x => x.QuoteAdditionalPayments)
            .FirstOrDefaultAsync(q => q.QuoteReference == quoteReference, cancellationToken);
    }

    public Task<Quote?> GetQuoteByStripeCheckoutSessionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        return db.Set<Quote>()
            .Include(x => x.InventoryItems)
            .Include(x => x.QuoteAdditionalPayments)
            .FirstOrDefaultAsync(q => q.StripeSessionId == sessionId, cancellationToken);
    }

    public async Task<(List<Quote> Quotes, int TotalCount)> GetAdminQuotesAsync(
        int page,
        int pageSize,
        string? search = null,
        string? sortBy = "createdAt",
        string? sortDir = "desc",
        string? status = null,
        LocalDate? dateFrom = null,
        LocalDate? dateTo = null,
        CancellationToken ct = default)
    {
        // Start with base query - NO includes yet for better performance
        var baseQuery = db.Set<Quote>().AsQueryable();

        // Apply filters first (most selective)
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<PaymentStatus>(status, true, out var paymentStatus))
            {
                baseQuery = baseQuery.Where(q => q.PaymentStatus == paymentStatus);
            }
        }

        if (dateFrom.HasValue)
        {
            var fromInclusive = dateFrom.Value.AtStartOfDayInZone(Utc).ToInstant();
            baseQuery = baseQuery.Where(q => q.CreatedAt >= fromInclusive);
        }

        if (dateTo.HasValue)
        {
            var toExclusive = dateTo.Value.PlusDays(1).AtStartOfDayInZone(Utc).ToInstant();
            baseQuery = baseQuery.Where(q => q.CreatedAt < toExclusive);
        }

        // Note: Driver filtering removed for performance - will be handled in detail view
        // if (driverId.HasValue) { ... } - REMOVED for performance

        // Apply search filter - ONLY on Quote fields for maximum performance
        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery = baseQuery.Where(q =>
                q.QuoteReference.Contains(search));
            // Customer/Driver search removed for performance - will be handled in detail view
        }

        // Get total count BEFORE adding includes (much faster)
        var totalCount = await baseQuery.CountAsync(ct);

        // Apply sorting - ONLY on Quote fields for maximum performance
        baseQuery = sortBy.ToLower() switch
        {
            "createdat" => sortDir.ToLower() == "asc"
                ? baseQuery.OrderBy(q => q.CreatedAt)
                : baseQuery.OrderByDescending(q => q.CreatedAt),
            "amount" => sortDir.ToLower() == "asc"
                ? baseQuery.OrderBy(q => q.TotalCost)
                : baseQuery.OrderByDescending(q => q.TotalCost),
            "status" => sortDir.ToLower() == "asc"
                ? baseQuery.OrderBy(q => q.PaymentStatus)
                : baseQuery.OrderByDescending(q => q.PaymentStatus),
            // Driver name sorting removed for performance - will be handled in detail view
            _ => baseQuery.OrderByDescending(q => q.CreatedAt) // Default sorting
        };

        // Apply pagination - Include QuoteAdditionalPayments for proper total cost calculation
        var quotes = await baseQuery
            .Include(q => q.QuoteAdditionalPayments) // Include additional payments for total cost calculation
            .Select(q => new Quote
            {
                Id = q.Id,
                QuoteReference = q.QuoteReference,
                TotalCost = q.TotalCost,
                PaymentStatus = q.PaymentStatus,
                CreatedAt = q.CreatedAt,
                Type = q.Type,
                VanType = q.VanType,
                PaymentType = q.PaymentType,
                // Include QuoteAdditionalPayments for total cost calculation
                QuoteAdditionalPayments = q.QuoteAdditionalPayments,
                // Exclude other navigation properties for performance
                SessionId = q.SessionId,
                DistanceMiles = q.DistanceMiles,
                NumberOfItemsToDismantle = q.NumberOfItemsToDismantle,
                NumberOfItemsToAssemble = q.NumberOfItemsToAssemble,
                DriverCount = q.DriverCount,
                CollectionDate = q.CollectionDate,
                DeliveryDate = q.DeliveryDate,
                Hours = q.Hours,
                FlexibleTime = q.FlexibleTime,
                TimeSlot = q.TimeSlot,
                PricingTier = q.PricingTier,
                PaymentMethodId = q.PaymentMethodId,
                PaymentIntentId = q.PaymentIntentId,
                DepositAmount = q.DepositAmount,
                ReceiptUrl = q.ReceiptUrl,
                DueDate = q.DueDate,
                Version = q.Version,
                StripeSessionId = q.StripeSessionId,
                CreatedBy = q.CreatedBy,
                ModifiedAt = q.ModifiedAt,
                ModifiedBy = q.ModifiedBy
                // Origin, Destination, InventoryItems, CustomerQuotes, DriverQuotes excluded for performance
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (quotes, totalCount);
    }

    public async Task<Quote?> GetAdminQuoteDetailsAsync(Guid quoteId, CancellationToken ct = default)
    {
        return await db.Set<Quote>()
            .Include(q => q.CustomerQuotes)
                .ThenInclude(cq => cq.User)
                    .ThenInclude(u => u.BillingAddress)
            .Include(q => q.DriverQuotes)
                .ThenInclude(dq => dq.User)
            .Include(q => q.Origin)
            .Include(q => q.Destination)
            .Include(q => q.InventoryItems)
            .Include(q => q.QuoteAdditionalPayments)
            .FirstOrDefaultAsync(q => q.Id == quoteId, ct);
    }

    public Task<List<Quote>> GetPayLaterQuotesForTodayAsync(LocalDate today, CancellationToken cancellationToken)
    {
        return db.Set<Quote>()
            .Where(x => x.PaymentType == PaymentType.Later &&
                        x.PaymentStatus == PaymentStatus.PaymentSetup &&
                        x.DueDate.HasValue &&
                        today >= x.DueDate.Value)
            .ToListAsync(cancellationToken);
    }

}
