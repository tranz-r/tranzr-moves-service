using ErrorOr;
using Mediator;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Features.AdditionalPrices.Create;

public record CreateAdditionalPriceCommand(
    AdditionalPriceType Type,
    string? Description,
    decimal Price,
    string CurrencyCode,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsActive) : IRequest<ErrorOr<AdditionalPriceDto>>;
