using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Quote.SelectQuoteType;

public class SelectQuoteTypeCommandHandler(
    IQuoteRepository quoteRepository,
    ILogger<SelectQuoteTypeCommandHandler> logger) 
    : ICommandHandler<SelectQuoteTypeCommand, ErrorOr<QuoteDto>>
{
    public async ValueTask<ErrorOr<QuoteDto>> Handle(
        SelectQuoteTypeCommand command, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.GuestId))
        {
            return Error.Custom((int)CustomErrorType.BadRequest, "GuestId.Required", "Guest ID is required");
        }

        try
        {
            // Ensure session exists
            await quoteRepository.CreateIfMissingAsync(command.GuestId, cancellationToken);
            
            // Get or create quote for the selected type
            var quote = await quoteRepository.GetOrCreateQuoteAsync(command.GuestId, command.QuoteType, cancellationToken);
            
            if (quote == null)
            {
                logger.LogError("Failed to create or retrieve quote for guest {GuestId} and type {QuoteType}", 
                    command.GuestId, command.QuoteType);
                return Error.Custom((int)CustomErrorType.InternalServerError, "Quote.CreationFailed", 
                    "Failed to create or retrieve quote");
            }

            // Note: Customer data is handled when saving quotes, not when selecting quote type

            logger.LogInformation("Successfully selected quote type {QuoteType} for guest {GuestId} with reference {QuoteReference}", 
                command.QuoteType, command.GuestId, quote.QuoteReference);

            var mapper = new QuoteMapper();
            
            var quoteDto = mapper.ToDto(quote);

            return quoteDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error selecting quote type {QuoteType} for guest {GuestId}", 
                command.QuoteType, command.GuestId);
            return Error.Custom((int)CustomErrorType.InternalServerError, "Quote.SelectionError", 
                "An error occurred while selecting the quote type");
        }
    }
}
