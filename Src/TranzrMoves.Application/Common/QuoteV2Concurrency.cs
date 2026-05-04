using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Common;

public static class QuoteV2Concurrency
{
    public static ErrorOr<bool> EnsureExpectedVersion(QuoteV2 quote, uint expectedVersion)
    {
        if (quote.Version != expectedVersion)
        {
            return Error.Conflict(
                QuoteV2Errors.ConcurrencyConflictCode,
                QuoteV2Errors.ConcurrencyConflictDescription);
        }

        return true;
    }
}
