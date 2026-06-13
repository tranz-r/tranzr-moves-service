namespace TranzrMoves.Notifications.Infrastructure;

public static class MarketingConsentEmailNormalizer
{
    public static string Normalize(string email) =>
        email.Trim().ToLowerInvariant();
}
