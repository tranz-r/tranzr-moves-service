using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.Quote.SelectQuoteType;

public record SelectQuoteTypeCommand(
    string GuestId,
    QuoteType QuoteType) : ICommand<ErrorOr<QuoteDto>>;
