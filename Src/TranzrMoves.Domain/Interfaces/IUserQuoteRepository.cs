using System.Collections.Immutable;
using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IUserQuoteRepository
{
    Task<ErrorOr<CustomerQuote>> AddUserJobAsync(CustomerQuote customerQuote,
        CancellationToken cancellationToken);

    Task<ErrorOr<List<CustomerQuote>>> AddUserQuotesAsync(List<CustomerQuote> customerQuotes,
        CancellationToken cancellationToken);

    Task<CustomerQuote?> GetUserJobAsync(Guid userJobId, CancellationToken cancellationToken);
    Task<ImmutableList<CustomerQuote>> GetUserJobsAsync(Guid userJobId, CancellationToken cancellationToken);

    // Task<ImmutableList<Quote>> GetJobsForCustomerAsync(Guid userId, IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken);

    Task<ErrorOr<CustomerQuote>> UpdateUserJobAsync(CustomerQuote customerQuote,
        CancellationToken cancellationToken);

    Task DeleteUserJobAsync(CustomerQuote customerQuote, CancellationToken cancellationToken);
}