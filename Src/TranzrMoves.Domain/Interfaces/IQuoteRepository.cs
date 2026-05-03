using ErrorOr;

using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteRepository
{
    // Session Management
    Task CreateIfMissingAsync(string guestId, CancellationToken ct = default);

    // Individual Quote Management
    Task<Quote?> GetQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
    Task<Quote?> GetQuoteAsync(Guid quoteId, CancellationToken ct = default);
    Task<Quote?> GetOrCreateQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
    Task<ErrorOr<Quote>> UpdateQuoteAsync(Quote quote, CancellationToken ct = default);
    Task<bool> DeleteQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
    Task<Quote?> GetQuoteByReferenceAsync(string quoteReference, string paymentIntentId, CancellationToken cancellationToken = default);
    Task<Quote?> GetQuoteByReferenceAsync(string quoteReference, CancellationToken cancellationToken = default);
    Task<Quote?> GetQuoteByStripeCheckoutSessionIdAsync(string sessionId, CancellationToken cancellationToken);

    // Admin Management
    Task<(List<Quote> Quotes, int TotalCount)> GetAdminQuotesAsync(
        int page,
        int pageSize,
        string? search = null,
        string? sortBy = "createdAt",
        string? sortDir = "desc",
        string? status = null,
        LocalDate? dateFrom = null,
        LocalDate? dateTo = null,
        CancellationToken ct = default);

    // Admin Quote Details
    Task<Quote?> GetAdminQuoteDetailsAsync(Guid quoteId, CancellationToken ct = default);
    Task<List<Quote>> GetPayLaterQuotesForTodayAsync(LocalDate today, CancellationToken cancellationToken);
    /// <summary>
    /// Persists tracked changes. Returns a concurrency conflict when the database row version no longer matches (e.g. concurrent update).
    /// </summary>
    Task<ErrorOr<bool>> SaveChangesAsync(CancellationToken ct);
    Task<QuoteV2?> GetQuoteByIdAsync(Guid quoteId, CancellationToken ct, bool asTracking = false);

    Task<QuoteV2?> GetQuoteV2ByQuoteReferenceAsync(string quoteReference, CancellationToken ct,
        bool asTracking = false);

    Task<QuoteV2> GetOrCreateQuoteV2Async(string guestId, QuoteType quoteType, CancellationToken ct = default);

    /// <summary>
    /// QuoteV2 pay-later candidates: payment setup complete, Later payment with saved method and due date reached (UTC calendar day).
    /// </summary>
    Task<List<QuoteV2>> GetPayLaterQuoteV2sForTodayAsync(LocalDate today, CancellationToken ct = default);
}


