using System.Collections.Immutable;
using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IUserQuoteRepository
{
    Task<ErrorOr<CustomerQuote>> AddUserQuoteAsync(CustomerQuote customerQuote,
        CancellationToken cancellationToken);

    Task<ErrorOr<List<CustomerQuote>>> AddUserQuotesAsync(List<CustomerQuote> customerQuotes,
        CancellationToken cancellationToken);

    Task<CustomerQuote?> GetUserQuoteAsync(Guid userQuoteId, CancellationToken cancellationToken);
    Task<ImmutableList<CustomerQuote>> GetUserQuotesAsync(Guid userQuoteId, CancellationToken cancellationToken);

    // Get customer quote relationship by quote ID
    Task<CustomerQuote?> GetUserQuoteByQuoteIdAsync(Guid quoteId, CancellationToken cancellationToken);

    // Task<ImmutableList<Quote>> GetJobsForCustomerAsync(Guid userId, IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken);

    Task<ErrorOr<CustomerQuote>> UpdateUserQuoteAsync(CustomerQuote customerQuote,
        CancellationToken cancellationToken);

    Task DeleteUserQuoteAsync(CustomerQuote customerQuote, CancellationToken cancellationToken);
}
