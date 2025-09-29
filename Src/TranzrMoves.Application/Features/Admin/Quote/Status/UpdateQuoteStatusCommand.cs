using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Admin.Quote.Status;

public record UpdateQuoteStatusCommand(
    Guid QuoteId,
    string Status,
    string? Reason = null,
    string? AdminNote = null,
    bool NotifyCustomer = false,
    bool NotifyDriver = false) : ICommand<ErrorOr<UpdateQuoteStatusResponse>>;

public record UpdateQuoteStatusResponse(
    bool Success,
    string Message,
    UpdatedQuoteDto UpdatedQuote);

public record UpdatedQuoteDto(
    Guid Id,
    string Status,
    DateTimeOffset ModifiedAt,
    string ModifiedBy);

public class UpdateQuoteStatusCommandHandler(
    IQuoteRepository quoteRepository,
    ILogger<UpdateQuoteStatusCommandHandler> logger) : ICommandHandler<UpdateQuoteStatusCommand, ErrorOr<UpdateQuoteStatusResponse>>
{
    public async ValueTask<ErrorOr<UpdateQuoteStatusResponse>> Handle(UpdateQuoteStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Updating quote {QuoteId} status to {Status}", request.QuoteId, request.Status);

            var quote = await quoteRepository.GetQuoteAsync(request.QuoteId, cancellationToken);

            if (quote == null)
            {
                logger.LogWarning("Quote {QuoteId} not found", request.QuoteId);
                return Error.NotFound("Quote.NotFound", $"Quote with ID {request.QuoteId} not found");
            }

            // Validate status
            if (!Enum.TryParse<Domain.Entities.PaymentStatus>(request.Status, true, out var paymentStatus))
            {
                logger.LogWarning("Invalid status {Status} for quote {QuoteId}", request.Status, request.QuoteId);
                return Error.Validation("QuoteStatus.Invalid", $"Invalid status: {request.Status}");
            }

            // Update quote status
            quote.PaymentStatus = paymentStatus;
            quote.ModifiedAt = DateTimeOffset.UtcNow;
            quote.ModifiedBy = "Admin"; // TODO: Get actual admin user

            await quoteRepository.UpdateQuoteAsync(quote, cancellationToken);

            logger.LogInformation("Successfully updated quote {QuoteId} status to {Status}", request.QuoteId, request.Status);

            return new UpdateQuoteStatusResponse(
                true,
                $"Quote status updated to {request.Status}",
                new UpdatedQuoteDto(
                    quote.Id,
                    request.Status,
                    quote.ModifiedAt,
                    quote.ModifiedBy));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating quote {QuoteId} status", request.QuoteId);
            return Error.Failure("UpdateQuoteStatus.Failed", "Failed to update quote status");
        }
    }
}
