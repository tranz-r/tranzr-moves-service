using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteRepository
{
    // Session Management
    Task CreateIfMissingAsync(string guestId, CancellationToken ct = default);
    
    // Individual Quote Management
    Task<Quote?> GetQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
    Task<Quote?> GetOrCreateQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
    Task<Quote?> UpsertQuoteAsync(string guestId, Quote quote, uint? providedVersion, CancellationToken ct = default);
    Task<bool> DeleteQuoteAsync(string guestId, QuoteType quoteType, CancellationToken ct = default);
}


