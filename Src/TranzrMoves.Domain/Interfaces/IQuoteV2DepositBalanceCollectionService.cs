using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteV2DepositBalanceCollectionService
{
    Task<ErrorOr<Success>> TryCollectAsync(Guid quoteId, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> CollectAsync(
        QuoteV2 quote,
        decimal? extraCharges = null,
        string? extraChargesDescription = null,
        CancellationToken cancellationToken = default);
}
