using ErrorOr;
using Mediator;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.Quote.SelectQuoteType;

public record SelectQuoteTypeCommand(
    string GuestId,
    QuoteType QuoteType) : ICommand<ErrorOr<SelectQuoteTypeResponse>>;

public record SelectQuoteTypeResponse(
    Guid Id,
    string Type,
    string QuoteReference,
    string SessionId);
