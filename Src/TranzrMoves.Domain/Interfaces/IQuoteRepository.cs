using ErrorOr;

using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteRepository
{
    /// <summary>
    /// Persists tracked changes. Returns a concurrency conflict when the database row version no longer matches (e.g. concurrent update).
    /// </summary>
    Task<ErrorOr<bool>> SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Stages a new payment row for insert. Required when adding payments to a tracked quote because the DbContext uses NoTracking by default.
    /// </summary>
    void AddPayment(Payment payment);
    Task<QuoteV2?> GetQuoteByIdAsync(Guid quoteId, CancellationToken ct, bool asTracking = false);

    Task<QuoteV2?> GetQuoteV2ByQuoteReferenceAsync(string quoteReference, CancellationToken ct,
        bool asTracking = false);

    Task<QuoteV2> GetOrCreateQuoteV2Async(string guestId, QuoteType quoteType, CancellationToken ct = default);

    /// <summary>
    /// QuoteV2 pay-later candidates: payment setup complete, Later payment with saved method and due date reached (UTC calendar day).
    /// </summary>
    Task<List<QuoteV2>> GetPayLaterQuoteV2sForTodayAsync(LocalDate today, CancellationToken ct = default);

    /// <summary>
    /// Pay-later candidates due for collection, excluding quotes with a paid Balance payment row.
    /// </summary>
    Task<List<QuoteV2>> GetPayLaterQuoteV2sDueForCollectionAsync(LocalDate today, CancellationToken ct = default);

    /// <summary>
    /// Deposit balance candidates: partially paid with paid deposit and collection due date on or before the given London calendar day.
    /// Caller must filter same-day quotes where current time is before 00:05 London.
    /// </summary>
    Task<List<QuoteV2>> GetDepositQuoteV2sDueForBalanceCollectionAsync(LocalDate todayInLondon,
        CancellationToken ct = default);

    /// <summary>
    /// Incomplete quotes eligible for a transactional quote-reminder email.
    /// </summary>
    Task<List<QuoteV2>> GetQuotesDueForReminderAsync(
        Instant idleBefore,
        Instant cooldownBefore,
        Instant now,
        CancellationToken ct = default);
}


