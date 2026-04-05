namespace TranzrMoves.Application.Common.Time;

public sealed class TimeService(IClock clock) : ITimeService
{
    public Instant Now() => clock.GetCurrentInstant();

    public ZonedDateTime NowInUtc() => clock.GetCurrentInstant().InUtc();

    public ZonedDateTime NowInZone(string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId)
            ?? throw new ArgumentException($"Unknown time zone id: '{timeZoneId}'.", nameof(timeZoneId));
        return clock.GetCurrentInstant().InZone(zone);
    }

    public LocalDate TodayIn(string timeZoneId) => NowInZone(timeZoneId).Date;

    public LocalDate TodayInUtc() => NowInUtc().Date;
}
