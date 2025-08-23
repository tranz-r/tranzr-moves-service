using ErrorOr;
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
        var (driverId, quoteId) = (command.Request.DriverId, JobId: command.Request.QuoteId);

        var existing = await driverQuoteRepository.GetDriverQuoteAsync(driverId, quoteId, cancellationToken);
        if (existing is null)
        {
            return Error.Custom((int)CustomErrorType.NotFound, "DriverJob.NotFound", "Assignment not found");
        }

        await driverQuoteRepository.DeleteDriverQuoteAsync(existing, cancellationToken);
        return true;
    }
}
