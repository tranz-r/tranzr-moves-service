using System.Collections.Immutable;
using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IDriverJobRepository
{
    Task<ErrorOr<DriverJob>> AddDriverJobAsync(DriverJob driverJob, CancellationToken cancellationToken);

    Task<DriverJob?> GetDriverJobAsync(Guid driverJobId, CancellationToken cancellationToken);

    Task<DriverJob?> GetDriverJobAsync(Guid driverId, Guid jobId, CancellationToken cancellationToken);

    Task<ImmutableList<Job>> GetJobsForDriverAsync(Guid driverId, IEnumerable<PaymentStatus>? statuses, CancellationToken cancellationToken);

    Task DeleteDriverJobAsync(DriverJob driverJob, CancellationToken cancellationToken);
}
