using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Quote.Create;

public record CreateQuotesCommand(
    QuoteContextDto QuoteContextDto,
    string GuestId,
    string Etag) : ICommand<ErrorOr<(UserDto? User, List<QuoteDto> Quotes, string Etag)>>;