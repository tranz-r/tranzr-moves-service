using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IAdditionalPriceRepository
{
    Task<ErrorOr<AdditionalPrice>> AddAdditionalPriceAsync(AdditionalPrice additionalPrice, CancellationToken cancellationToken);
    Task<AdditionalPrice?> GetAdditionalPriceAsync(Guid id, CancellationToken cancellationToken);
    Task<List<AdditionalPrice>> GetAdditionalPricesAsync(bool? isActive, CancellationToken cancellationToken);
    Task<ErrorOr<AdditionalPrice>> UpdateAdditionalPriceAsync(AdditionalPrice additionalPrice, CancellationToken cancellationToken);
    Task DeleteAdditionalPriceAsync(AdditionalPrice additionalPrice, CancellationToken cancellationToken);
}
