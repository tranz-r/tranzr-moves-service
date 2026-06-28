namespace TranzrMoves.Infrastructure.Services;

public sealed class SupabaseAuthOptions
{
    public string? Url { get; set; }

    public string? ServiceRoleKey { get; set; }

    public string? InviteRedirectUrl { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(ServiceRoleKey);

    public string AuthUrl => Url!.TrimEnd('/') + "/auth/v1";
}
