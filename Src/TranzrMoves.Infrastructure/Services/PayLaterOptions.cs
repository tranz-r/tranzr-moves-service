namespace TranzrMoves.Infrastructure.Services;

public sealed class PayLaterOptions
{
    public const string SectionName = "PayLater";

    public int RecoveryIntervalMinutes { get; set; } = 30;

    public bool UseDurableMessaging { get; set; } = true;

    /// <summary>IANA time zone for deposit balance charge at 00:05 on the move date. Default Europe/London.</summary>
    public string? DepositChargeTimeZone { get; set; }
}
