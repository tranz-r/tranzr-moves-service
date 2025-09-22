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
}


