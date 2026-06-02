namespace TranzrMoves.Notifications.Infrastructure.Options;

public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    public bool UseDurableMessaging { get; set; } = true;

    public string EmailProvider { get; set; } = "Acs";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 2525;

    public bool UseSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }
}
