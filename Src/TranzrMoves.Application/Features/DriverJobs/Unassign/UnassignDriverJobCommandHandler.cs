using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.DriverJobs.Unassign;

public class UnassignDriverJobCommandHandler(
    IDriverJobRepository driverJobRepository,
    ILogger<UnassignDriverJobCommandHandler> logger
) : ICommandHandler<UnassignDriverJobCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(UnassignDriverJobCommand command, CancellationToken cancellationToken)
    {
        var (driverId, jobId) = (command.Request.DriverId, command.Request.JobId);

        var existing = await driverJobRepository.GetDriverJobAsync(driverId, jobId, cancellationToken);
        if (existing is null)
        {
            return Error.Custom((int)CustomErrorType.NotFound, "DriverJob.NotFound", "Assignment not found");
        }

        await driverJobRepository.DeleteDriverJobAsync(existing, cancellationToken);
        return true;
    }
}
