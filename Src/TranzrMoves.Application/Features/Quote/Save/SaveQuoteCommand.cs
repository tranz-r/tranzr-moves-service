using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Quote.Save;

public record SaveQuoteCommand(
    QuoteDto Quote,
    UserDto? Customer,
    string? ETag) : ICommand<ErrorOr<SaveQuoteResponse>>;

public record SaveQuoteResponse(QuoteDto Quote, UserDto? Customer, string ETag);

