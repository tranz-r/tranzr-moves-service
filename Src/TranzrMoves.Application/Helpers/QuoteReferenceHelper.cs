using NodaTime.Text;
using TranzrMoves.Application.Common.Time;

namespace TranzrMoves.Application.Helpers;

/// <summary>
/// Builds quote references: <c>TRZ-{yyMMdd}-{3 hex}</c> (UTC calendar date).
/// The suffix has 4,096 values per day; rely on the unique DB index and retry on duplicate if volume is high.
/// </summary>
public static class QuoteReferenceHelper
{
    public static string GenerateQuoteReference(ITimeService time) =>
        GenerateQuoteReference(time.TodayInUtc());

    /// <param name="utcToday">UTC calendar date used for the middle segment.</param>
    public static string GenerateQuoteReference(LocalDate utcToday)
    {
        var datePart = LocalDatePattern.CreateWithInvariantCulture("yyMMdd").Format(utcToday);
        return $"TRZ-{datePart}-{Guid.NewGuid().ToString("N")[..3].ToUpperInvariant()}";
    }
}
