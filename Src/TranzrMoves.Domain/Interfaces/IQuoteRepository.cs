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
}


