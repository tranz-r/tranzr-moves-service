using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Features.DriverJobs.Assign;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.DriverJobs.Assign;

public class AssignDriverJobCommandHandler(
    IDriverJobRepository driverJobRepository,
    IJobRepository jobRepository,
    IUserRepository userRepository,
    ILogger<AssignDriverJobCommandHandler> logger
) : ICommandHandler<AssignDriverJobCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(AssignDriverJobCommand command, CancellationToken cancellationToken)
    {
        var (driverId, jobId) = (command.Request.DriverId, command.Request.JobId);

        var user = await userRepository.GetUserAsync(driverId, cancellationToken);
        if (user is null)
        {
            return Error.Custom((int)CustomErrorType.NotFound, "User.NotFound", "Driver not found");
        }

        var job = await jobRepository.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return Error.Custom((int)CustomErrorType.NotFound, "Job.NotFound", "Job not found");
        }

        var existing = await driverJobRepository.GetDriverJobAsync(driverId, jobId, cancellationToken);
        if (existing is not null)
        {
            return Error.Conflict(description: "Driver already assigned to this job");
        }

        var result = await driverJobRepository.AddDriverJobAsync(new DriverJob
        {
            UserId = driverId,
            JobId = jobId
        }, cancellationToken);

        if (result.IsError)
        {
            return Error.Conflict(description: result.FirstError.Description);
        }

        return true;
    }
}
