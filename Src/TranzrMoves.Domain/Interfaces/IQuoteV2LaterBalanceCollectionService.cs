using ErrorOr;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Domain.Interfaces;

public interface IQuoteV2LaterBalanceCollectionService
{
    Task<ErrorOr<Success>> CollectAsync(QuoteV2 quote, CancellationToken cancellationToken);
}
