using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IRateCardRepository
{
    Task<ErrorOr<RateCard>> AddRateCardAsync(RateCard rateCard, CancellationToken cancellationToken);
    Task<RateCard?> GetRateCardAsync(Guid id, CancellationToken cancellationToken);
    Task<List<RateCard>> GetRateCardsAsync(bool? isActive, CancellationToken cancellationToken);
    Task<ErrorOr<RateCard>> UpdateRateCardAsync(RateCard rateCard, CancellationToken cancellationToken);
    Task DeleteRateCardAsync(RateCard rateCard, CancellationToken cancellationToken);
}
