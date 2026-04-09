using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.DriverJobs.Unassign;

public class UnassignDriverJobCommandHandler(
    IDriverQuoteRepository driverQuoteRepository,
    ILogger<UnassignDriverJobCommandHandler> logger
) : ICommandHandler<UnassignDriverJobCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(UnassignDriverJobCommand command, CancellationToken cancellationToken)
    {
        var (driverId, quoteId) = (command.Request.DriverId, command.Request.QuoteId);

        var existing = await driverQuoteRepository.GetDriverQuoteAsync(driverId, quoteId, cancellationToken);
        if (existing is null)
        {
            logger.LogWarning("Unassign driver job: no assignment for driver {DriverId} on quote {QuoteId}", driverId,
                quoteId);
            return Error.Custom((int)CustomErrorType.NotFound, "DriverJob.NotFound", "Assignment not found");
        }

        await driverQuoteRepository.DeleteDriverQuoteAsync(existing, cancellationToken);
        logger.LogInformation("Driver {DriverId} unassigned from quote {QuoteId}", driverId, quoteId);
        return true;
    }
}
