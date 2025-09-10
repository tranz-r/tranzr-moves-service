using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IDriverQuoteRepository
{
    Task<ErrorOr<DriverQuote>> AddDriverQuoteAsync(DriverQuote driverQuote, CancellationToken cancellationToken);

    Task<DriverQuote?> GetDriverQuoteAsync(Guid driverQuoteId, CancellationToken cancellationToken);

    Task<DriverQuote?> GetDriverQuoteAsync(Guid driverId, Guid jobId, CancellationToken cancellationToken);

    // Task<ImmutableList<Quote>> GetQuotesForDriverAsync(Guid driverId, IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken);

    Task DeleteDriverQuoteAsync(DriverQuote driverQuote, CancellationToken cancellationToken);
}
