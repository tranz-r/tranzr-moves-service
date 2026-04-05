namespace TranzrMoves.Application.Common.Time;

/// <summary>Central access to the current time (inject <see cref="IClock"/> in tests).</summary>
public interface ITimeService
{
    Instant Now();

    ZonedDateTime NowInUtc();

    ZonedDateTime NowInZone(string timeZoneId);

    LocalDate TodayIn(string timeZoneId);

    /// <summary>UTC calendar date (same as <see cref="NowInUtc"/>.Date).</summary>
    LocalDate TodayInUtc();
}
