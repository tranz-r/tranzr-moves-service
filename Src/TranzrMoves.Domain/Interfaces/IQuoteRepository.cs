using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteRepository
{
    // Session Management
    Task CreateIfMissingAsync(string guestId, CancellationToken ct = default);
    Task<QuoteSession?> GetSessionAsync(string guestId, CancellationToken ct = default);
    
    // QuoteContext State Management (Primary interface)
    Task<List<Quote>> GetQuoteContextStateAsync(string guestId, CancellationToken ct = default);

    Task<List<Quote>> SaveQuoteContextStateAsync(string guestId, Dictionary<QuoteType, Quote> quotes,
        string? providedEtag, CancellationToken ct = default);
    
    // Individual Quote Management
    Task<Quote?> GetQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
    Task<Quote?> GetOrCreateQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
    Task<List<Quote>> GetQuotesForSessionAsync(string guestId, CancellationToken ct = default);
    Task<Quote?> UpsertQuoteAsync(string guestId, Quote quote, string? providedEtag, CancellationToken ct = default);
    Task<bool> DeleteQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
}


