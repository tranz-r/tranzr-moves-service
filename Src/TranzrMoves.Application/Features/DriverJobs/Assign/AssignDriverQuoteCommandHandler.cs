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
            logger.LogWarning("Assign driver to quote: driver user {DriverId} not found", driverId);
            return Error.Custom((int)CustomErrorType.NotFound, "User.NotFound", "Driver not found");
        }

        var quote = await quoteRepository.GetQuoteAsync(quoteId, cancellationToken);
        if (quote is null)
        {
            logger.LogWarning("Assign driver to quote: quote {QuoteId} not found", quoteId);
            return Error.Custom((int)CustomErrorType.NotFound, "Job.NotFound", "Job not found");
        }

        var existing = await driverQuoteRepository.GetDriverQuoteAsync(driverId, quoteId, cancellationToken);
        if (existing is not null)
        {
            logger.LogWarning("Assign driver to quote: driver {DriverId} already assigned to quote {QuoteId}", driverId,
                quoteId);
            return Error.Conflict(description: "Driver already assigned to this job");
        }

        var result = await driverQuoteRepository.AddDriverQuoteAsync(new DriverQuote
        {
            UserId = driverId,
            QuoteId = quoteId,
            User = user,
            Quote = quote
        }, cancellationToken);

        if (result.IsError)
        {
            logger.LogWarning("Assign driver to quote: persistence conflict for driver {DriverId} quote {QuoteId}: {Error}",
                driverId, quoteId, result.FirstError.Description);
            return Error.Conflict(description: result.FirstError.Description);
        }

        logger.LogInformation("Driver {DriverId} assigned to quote {QuoteId}", driverId, quoteId);
        return true;
    }
}
