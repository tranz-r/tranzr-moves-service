using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.DriverJobs.Assign;

public class AssignDriverJobCommandHandler(
    IDriverQuoteRepository driverQuoteRepository,
    IQuoteRepository quoteRepository,
    IUserRepository userRepository,
    ILogger<AssignDriverJobCommandHandler> logger
) : ICommandHandler<AssignDriverQuoteCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(AssignDriverQuoteCommand command, CancellationToken cancellationToken)
    {
        var (driverId, jobId) = (command.Request.DriverId, command.Request.JobId);

        var user = await userRepository.GetUserAsync(driverId, cancellationToken);
        if (user is null)
        {
            return Error.Custom((int)CustomErrorType.NotFound, "User.NotFound", "Driver not found");
        }

        var job = await driverQuoteRepository.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return Error.Custom((int)CustomErrorType.NotFound, "Job.NotFound", "Job not found");
        }

        var existing = await driverQuoteRepository.GetDriverJobAsync(driverId, jobId, cancellationToken);
        if (existing is not null)
        {
            return Error.Conflict(description: "Driver already assigned to this job");
        }

        var result = await driverQuoteRepository.AddDriverJobAsync(new DriverQuote
        {
            UserId = driverId,
            QuoteId = jobId
        }, cancellationToken);

        if (result.IsError)
        {
            return Error.Conflict(description: result.FirstError.Description);
        }

        return true;
    }
}
