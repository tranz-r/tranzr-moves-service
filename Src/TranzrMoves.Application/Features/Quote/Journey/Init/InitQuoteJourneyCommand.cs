using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.Quote.Journey.Init;

public sealed record InitQuoteJourneyCommand(
    string GuestId,
    QuoteType QuoteType) : ICommand<ErrorOr<QuoteJourneyResponse>>;
