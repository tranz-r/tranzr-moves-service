using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.DriverJobs.Assign;

public class AssignDriverQuoteCommandHandler(
    IDriverQuoteRepository driverQuoteRepository,
    IQuoteRepository quoteRepository,
    IUserRepository userRepository,
    ILogger<AssignDriverQuoteCommandHandler> logger
) : ICommandHandler<AssignDriverQuoteCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(AssignDriverQuoteCommand command, CancellationToken cancellationToken)
    {
        var (driverId, quoteId) = (command.Request.DriverId, command.Request.QuoteId);

        var user = await userRepository.GetUserAsync(driverId, cancellationToken);
        if (user is null)
        {
            return Error.Custom((int)CustomErrorType.NotFound, "User.NotFound", "Driver not found");
        }

        var job = await driverQuoteRepository.GetDriverQuoteAsync(quoteId, cancellationToken);
        if (job is null)
        {
            return Error.Custom((int)CustomErrorType.NotFound, "Job.NotFound", "Job not found");
        }

        var existing = await driverQuoteRepository.GetDriverQuoteAsync(driverId, quoteId, cancellationToken);
        if (existing is not null)
        {
            return Error.Conflict(description: "Driver already assigned to this job");
        }

        var result = await driverQuoteRepository.AddDriverQuoteAsync(new DriverQuote
        {
            UserId = driverId,
            QuoteId = quoteId
        }, cancellationToken);

        if (result.IsError)
        {
            return Error.Conflict(description: result.FirstError.Description);
        }

        return true;
    }
}
