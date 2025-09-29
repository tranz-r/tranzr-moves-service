using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Admin.Quote.Driver;

public record UnassignDriverCommand(
    Guid QuoteId,
    Guid DriverId,
    string? Reason = null,
    string? AdminNote = null,
    bool NotifyDriver = false,
    bool NotifyCustomer = false) : ICommand<ErrorOr<UnassignDriverResponse>>;

public record UnassignDriverResponse(
    bool Success,
    string Message,
    UnassignedDriverDto UnassignedDriver);

public record UnassignedDriverDto(
    Guid QuoteId,
    Guid DriverId,
    string DriverName,
    DateTimeOffset UnassignedAt,
    string UnassignedBy);

public class UnassignDriverCommandHandler(
    IQuoteRepository quoteRepository,
    IUserRepository userRepository,
    ILogger<UnassignDriverCommandHandler> logger) : ICommandHandler<UnassignDriverCommand, ErrorOr<UnassignDriverResponse>>
{
    public async ValueTask<ErrorOr<UnassignDriverResponse>> Handle(UnassignDriverCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Unassigning driver {DriverId} from quote {QuoteId}", request.DriverId, request.QuoteId);

            var quote = await quoteRepository.GetByIdAsync(request.QuoteId, cancellationToken);

            if (quote == null)
            {
                logger.LogWarning("Quote {QuoteId} not found", request.QuoteId);
                return Error.NotFound("Quote.NotFound", $"Quote with ID {request.QuoteId} not found");
            }

            var driver = await userRepository.GetByIdAsync(request.DriverId, cancellationToken);

            if (driver == null)
            {
                logger.LogWarning("Driver {DriverId} not found", request.DriverId);
                return Error.NotFound("Driver.NotFound", $"Driver with ID {request.DriverId} not found");
            }

            // Find and remove driver assignment
            var driverQuote = quote.DriverQuotes?.FirstOrDefault(dq => dq.UserId == request.DriverId);

            if (driverQuote == null)
            {
                logger.LogWarning("Driver {DriverId} is not assigned to quote {QuoteId}", request.DriverId, request.QuoteId);
                return Error.Validation("Driver.NotAssigned", "Driver is not assigned to this quote");
            }

            quote.DriverQuotes?.Remove(driverQuote);

            quote.ModifiedAt = DateTimeOffset.UtcNow;
            quote.ModifiedBy = "Admin"; // TODO: Get actual admin user

            await quoteRepository.UpdateAsync(quote, cancellationToken);

            logger.LogInformation("Successfully unassigned driver {DriverId} from quote {QuoteId}", request.DriverId, request.QuoteId);

            return new UnassignDriverResponse(
                true,
                $"Driver {driver.FullName} unassigned from quote",
                new UnassignedDriverDto(
                    quote.Id,
                    driver.Id,
                    driver.FullName ?? "",
                    DateTimeOffset.UtcNow,
                    "Admin"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unassigning driver {DriverId} from quote {QuoteId}", request.DriverId, request.QuoteId);
            return Error.Failure("UnassignDriver.Failed", "Failed to unassign driver from quote");
        }
    }
}


