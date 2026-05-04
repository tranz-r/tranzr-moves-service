namespace TranzrMoves.Domain.Constants;

/// <summary>
/// Error codes for QuoteV2 flows. <see cref="ConcurrencyConflictCode"/> maps to HTTP 412 in the API layer.
/// </summary>
public static class QuoteV2Errors
{
    public const string ConcurrencyConflictCode = "Quote.ConcurrencyConflict";

    public const string ConcurrencyConflictDescription =
        "The quote was updated elsewhere or your copy is out of date. Refresh journey state and try again.";
}
