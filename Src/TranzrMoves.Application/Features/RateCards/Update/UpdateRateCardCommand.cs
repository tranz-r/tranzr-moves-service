using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.RateCards.Update;

public record UpdateRateCardCommand(
    Guid Id,
    int Movers,
    ServiceLevel ServiceLevel,
    int BaseBlockHours,
    decimal BaseBlockPrice,
    decimal HourlyRateAfter,
    string CurrencyCode,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsActive) : IRequest<ErrorOr<RateCardDto>>;
