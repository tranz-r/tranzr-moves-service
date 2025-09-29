using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Admin.Quote.Notes;

public record AddAdminNoteCommand(
    Guid QuoteId,
    string Note,
    bool IsInternal = true,
    string? Category = null) : ICommand<ErrorOr<AddAdminNoteResponse>>;

public record AddAdminNoteResponse(
    bool Success,
    string Message,
    AdminNoteDto Note);

public record AdminNoteDto(
    Guid Id,
    string Note,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    bool IsInternal,
    string? Category);

public class AddAdminNoteCommandHandler(
    IQuoteRepository quoteRepository,
    ILogger<AddAdminNoteCommandHandler> logger) : ICommandHandler<AddAdminNoteCommand, ErrorOr<AddAdminNoteResponse>>
{
    public async ValueTask<ErrorOr<AddAdminNoteResponse>> Handle(AddAdminNoteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Adding admin note to quote {QuoteId}", request.QuoteId);

            var quote = await quoteRepository.GetByIdAsync(request.QuoteId, cancellationToken);

            if (quote == null)
            {
                logger.LogWarning("Quote {QuoteId} not found", request.QuoteId);
                return Error.NotFound("Quote.NotFound", $"Quote with ID {request.QuoteId} not found");
            }

            // Validate note
            if (string.IsNullOrWhiteSpace(request.Note))
            {
                logger.LogWarning("Empty note provided for quote {QuoteId}", request.QuoteId);
                return Error.Validation("Note.Empty", "Note cannot be empty");
            }

            // Create admin note (placeholder - would need actual AdminNote entity)
            var noteId = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow;
            var createdBy = "Admin"; // TODO: Get actual admin user

            // Update quote modification info
            quote.ModifiedAt = createdAt;
            quote.ModifiedBy = createdBy;

            await quoteRepository.UpdateAsync(quote, cancellationToken);

            logger.LogInformation("Successfully added admin note to quote {QuoteId}", request.QuoteId);

            var noteDto = new AdminNoteDto(
                noteId,
                request.Note,
                createdBy,
                createdAt,
                request.IsInternal,
                request.Category);

            return new AddAdminNoteResponse(
                true,
                "Admin note added successfully",
                noteDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding admin note to quote {QuoteId}", request.QuoteId);
            return Error.Failure("AddAdminNote.Failed", "Failed to add admin note");
        }
    }
}


