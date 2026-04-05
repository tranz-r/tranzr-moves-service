using NodaTime.Text;

namespace TranzrMoves.Application.Helpers;

/// <summary>
/// Formats quote references: <c>TRZ-{yyMMdd}-{n}</c> (UTC calendar date + DB sequence, no zero-padding).
/// </summary>
public static class QuoteReferenceHelper
{
    /// <param name="utcDate">UTC calendar date for the middle segment.</param>
    /// <param name="sequenceNumber">Value from PostgreSQL <c>nextval</c> on <c>quote_reference_seq</c>.</param>
    public static string FormatQuoteReference(LocalDate utcDate, long sequenceNumber)
    {
        var datePart = LocalDatePattern.CreateWithInvariantCulture("yyMMdd").Format(utcDate);
        return $"TRZ-{datePart}-{sequenceNumber}";
    }
}
