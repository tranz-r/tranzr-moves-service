using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Admin.Quote.Driver;

public record AssignDriverCommand(
    Guid QuoteId,
    Guid DriverId,
    string? Reason = null,
    string? AdminNote = null,
    bool NotifyDriver = false,
    bool NotifyCustomer = false) : ICommand<ErrorOr<AssignDriverResponse>>;

public record AssignDriverResponse(
    bool Success,
    string Message,
    AssignedDriverDto AssignedDriver);

public record AssignedDriverDto(
    Guid QuoteId,
    Guid DriverId,
    string DriverName,
    DateTimeOffset AssignedAt,
    string AssignedBy);

public class AssignDriverCommandHandler(
    IQuoteRepository quoteRepository,
    IUserRepository userRepository,
    ILogger<AssignDriverCommandHandler> logger) : ICommandHandler<AssignDriverCommand, ErrorOr<AssignDriverResponse>>
{
    public async ValueTask<ErrorOr<AssignDriverResponse>> Handle(AssignDriverCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Assigning driver {DriverId} to quote {QuoteId}", request.DriverId, request.QuoteId);

            var quote = await quoteRepository.GetQuoteAsync(request.QuoteId, cancellationToken);

            if (quote == null)
            {
                logger.LogWarning("Quote {QuoteId} not found", request.QuoteId);
                return Error.NotFound("Quote.NotFound", $"Quote with ID {request.QuoteId} not found");
            }

            var driver = await userRepository.GetUserAsync(request.DriverId, cancellationToken);

            if (driver == null)
            {
                logger.LogWarning("Driver {DriverId} not found", request.DriverId);
                return Error.NotFound("Driver.NotFound", $"Driver with ID {request.DriverId} not found");
            }

            // Check if driver has driver role
            if (driver.Role != Domain.Entities.Role.driver)
            {
                logger.LogWarning("User {DriverId} is not a driver", request.DriverId);
                return Error.Validation("Driver.InvalidRole", "User is not a driver");
            }

            // Remove existing driver assignments
            if (quote.DriverQuotes != null)
            {
                quote.DriverQuotes.Clear();
            }

            // Create new driver assignment
            var driverQuote = new DriverQuote
            {
                Id = Guid.NewGuid(),
                QuoteId = quote.Id,
                UserId = driver.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "Admin" // TODO: Get actual admin user
            };

            if (quote.DriverQuotes == null)
            {
                quote.DriverQuotes = new Collection<DriverQuote>();
            }
            quote.DriverQuotes.Add(driverQuote);

            quote.ModifiedAt = DateTimeOffset.UtcNow;
            quote.ModifiedBy = "Admin"; // TODO: Get actual admin user

            await quoteRepository.UpdateQuoteAsync(quote, cancellationToken);

            logger.LogInformation("Successfully assigned driver {DriverId} to quote {QuoteId}", request.DriverId, request.QuoteId);

            return new AssignDriverResponse(
                true,
                $"Driver {driver.FullName} assigned to quote",
                new AssignedDriverDto(
                    quote.Id,
                    driver.Id,
                    driver.FullName ?? "",
                    DateTimeOffset.UtcNow,
                    "Admin"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning driver {DriverId} to quote {QuoteId}", request.DriverId, request.QuoteId);
            return Error.Failure("AssignDriver.Failed", "Failed to assign driver to quote");
        }
    }
}
