using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Helpers;
using TranzrMoves.Application.Statics;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class QuoteRepository(TranzrMovesDbContext db, ITimeService timeService, ILogger<QuoteRepository> logger) : IQuoteRepository
{
    public void AddPayment(Payment payment) => db.Set<Payment>().Add(payment);

    public async Task<ErrorOr<bool>> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            _ = await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                logger.LogError(
                    "Concurrency on {EntityType}, state {State}",
                    entry.Metadata.Name,
                    entry.State);

                foreach (var property in entry.Properties)
                {
                    logger.LogError(
                        "{Property}: Original={Original}, Current={Current}, Modified={Modified}",
                        property.Metadata.Name,
                        property.OriginalValue,
                        property.CurrentValue,
                        property.IsModified);
                }
            }

            logger.LogWarning(ex, "Concurrency conflict while saving quote changes.");
            return Error.Conflict(
                QuoteV2Errors.ConcurrencyConflictCode,
                QuoteV2Errors.ConcurrencyConflictDescription);
        }
    }

    public Task<QuoteV2?> GetQuoteByIdAsync(Guid quoteId, CancellationToken ct, bool asTracking = false)
    {
        IQueryable<QuoteV2> query = db.Set<QuoteV2>();

        query = query
            .Include(x => x.Addresses)
            .Include(x => x.Pricings)
            .Include(x => x.Schedule)
            .Include(x => x.Payments)
            .Include(x => x.InventoryItems)
            .Include(x => x.Customer)
            .ThenInclude(c => c!.Addresses);

        if (asTracking)
        {
            query = query.AsTracking();
        }

        return query.FirstOrDefaultAsync(x => x.Id == quoteId, ct);
    }

    public Task<QuoteV2?> GetQuoteV2ByQuoteReferenceAsync(string quoteReference, CancellationToken ct,
        bool asTracking = false)
    {
        IQueryable<QuoteV2> query = db.Set<QuoteV2>();

        query = query
            .Include(x => x.Addresses)
            .Include(x => x.Pricings)
            .Include(x => x.Schedule)
            .Include(x => x.Payments)
            .Include(x => x.InventoryItems)
            .Include(x => x.Customer)
            .ThenInclude(c => c!.Addresses)
            .Where(x => x.QuoteReference == quoteReference);

        if (asTracking)
        {
            query = query.AsTracking();
        }

        return query.FirstOrDefaultAsync(ct);
    }

    public async Task<QuoteV2> GetOrCreateQuoteV2Async(string guestId, QuoteType quoteType, CancellationToken ct = default)
    {
        var quote = await db.Set<QuoteV2>()
            .Include(x => x.Addresses)
            .Include(x => x.Pricings)
            .Include(x => x.Schedule)
            .Include(x => x.Payments)
            .Include(x => x.InventoryItems)
            .Include(x => x.Customer)
            .ThenInclude(c => c!.Addresses)
            .FirstOrDefaultAsync(x => x.SessionId == guestId && x.Type == quoteType
                && x.LastCompletedStepKey != null && x.LastCompletedStepKey != QuoteStepKeys.Payment, ct);

        if (quote is not null)
        {
            return quote;
        }

        var utcToday = timeService.TodayInUtc();
        var sequence = await db.Database
            .SqlQueryRaw<long>(
                $"""SELECT nextval('{Db.SCHEMA}.{Db.Sequences.QuoteReference}') AS "Value" """)
            .SingleAsync(ct);

        quote = new QuoteV2
        {
            SessionId = guestId,
            Type = quoteType,
            QuoteReference = QuoteReferenceHelper.FormatQuoteReference(utcToday, sequence),
            VanType = VanType.largeVan,
            CrewCount = 1
        };

        db.Set<QuoteV2>().Add(quote);
        await db.SaveChangesAsync(ct);

        return quote;
    }

    public Task<List<QuoteV2>> GetPayLaterQuoteV2sForTodayAsync(LocalDate today, CancellationToken ct = default)
    {
        return QueryPayLaterQuoteV2sDueAsync(today, ct);
    }

    public Task<List<QuoteV2>> GetPayLaterQuoteV2sDueForCollectionAsync(LocalDate today, CancellationToken ct = default)
    {
        return QueryPayLaterQuoteV2sDueAsync(today, ct);
    }

    public Task<List<QuoteV2>> GetDepositQuoteV2sDueForBalanceCollectionAsync(LocalDate todayInLondon,
        CancellationToken ct = default)
    {
        return db.Set<QuoteV2>()
            .AsTracking()
            .Include(x => x.Schedule)
            .Include(x => x.Payments)
            .Include(x => x.Customer)
            .ThenInclude(c => c!.Addresses)
            .Where(q =>
                q.PaymentStatus == PaymentStatus.PartiallyPaid &&
                q.Payments != null &&
                q.Payments.Any(p =>
                    p.PaymentType == PaymentType.Deposit &&
                    p.Status == StripePaymentStatus.Paid &&
                    p.PaymentMethodId != null &&
                    p.DueDate != null &&
                    todayInLondon >= p.DueDate!.Value) &&
                !q.Payments.Any(p =>
                    p.PaymentType == PaymentType.Balance &&
                    p.Status == StripePaymentStatus.Paid))
            .ToListAsync(ct);
    }

    private Task<List<QuoteV2>> QueryPayLaterQuoteV2sDueAsync(LocalDate today, CancellationToken ct)
    {
        return db.Set<QuoteV2>()
            .AsTracking()
            .Include(x => x.Schedule)
            .Include(x => x.Payments)
            .Include(x => x.Customer)
            .ThenInclude(c => c!.Addresses)
            .Where(q =>
                q.PaymentStatus == PaymentStatus.PaymentSetup &&
                q.Payments != null &&
                q.Payments.Any(p =>
                    p.PaymentType == PaymentType.Later &&
                    p.PaymentMethodId != null &&
                    p.DueDate != null &&
                    today >= p.DueDate!.Value) &&
                !q.Payments.Any(p =>
                    p.PaymentType == PaymentType.Balance &&
                    p.Status == StripePaymentStatus.Paid))
            .ToListAsync(ct);
    }
}
