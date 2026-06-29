namespace TranzrMoves.Infrastructure.Services;

public sealed class SupabaseAuthOptions
{
    public string? Url { get; set; }

    /// <summary>
    /// The Supabase elevated/admin key used for server-side admin operations (invite/create user).
    /// Accepts the new secret key (<c>sb_secret_…</c>) or the legacy <c>service_role</c> JWT.
    /// </summary>
    public string? SecretKey { get; set; }

    public string? InviteRedirectUrl { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(SecretKey);

    public string AuthUrl => Url!.TrimEnd('/') + "/auth/v1";
}
