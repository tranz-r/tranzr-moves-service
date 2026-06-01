using NodaTime;

namespace TranzrMoves.Infrastructure.Services;

public static class BalanceChargeScheduling
{
    public const string DefaultDepositChargeTimeZoneId = "Europe/London";

    private static readonly LocalTime DepositChargeLocalTime = new(0, 5);

    public static Instant GetDepositChargeInstant(LocalDate collectionDate, string timeZoneId = DefaultDepositChargeTimeZoneId)
    {
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId)
                   ?? throw new ArgumentException($"Unknown time zone id: '{timeZoneId}'.", nameof(timeZoneId));
        return collectionDate.At(DepositChargeLocalTime).InZoneLeniently(zone).ToInstant();
    }

    public static bool IsDepositChargeDue(LocalDate collectionDate, Instant now,
        string timeZoneId = DefaultDepositChargeTimeZoneId)
    {
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId)
                   ?? throw new ArgumentException($"Unknown time zone id: '{timeZoneId}'.", nameof(timeZoneId));
        var todayInZone = now.InZone(zone).Date;
        if (collectionDate < todayInZone)
        {
            return true;
        }

        if (collectionDate > todayInZone)
        {
            return false;
        }

        return now >= GetDepositChargeInstant(collectionDate, timeZoneId);
    }
}
