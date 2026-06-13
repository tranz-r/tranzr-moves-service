using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace TranzrMoves.Notifications.Application.Services;

public sealed class MarketingPreferenceTokenPayload
{
    public Guid PrefId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Purpose { get; set; } = "marketing-preferences";
}

public interface IMarketingPreferenceTokenService
{
    string Create(Guid prefId, string email, TimeSpan lifetime);

    MarketingPreferenceTokenPayload Read(string token);
}

public sealed class MarketingPreferenceTokenService(IDataProtectionProvider dataProtectionProvider)
    : IMarketingPreferenceTokenService
{
    private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
        .CreateProtector("TranzrMoves.MarketingPreferences.v1")
        .ToTimeLimitedDataProtector();

    public string Create(Guid prefId, string email, TimeSpan lifetime)
    {
        var payload = new MarketingPreferenceTokenPayload
        {
            PrefId = prefId,
            Email = email.Trim()
        };

        var json = JsonSerializer.Serialize(payload);
        var protectedValue = _protector.Protect(json, lifetime);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedValue));
    }

    public MarketingPreferenceTokenPayload Read(string token)
    {
        var protectedBytes = WebEncoders.Base64UrlDecode(token);
        var protectedValue = Encoding.UTF8.GetString(protectedBytes);
        var json = _protector.Unprotect(protectedValue, out _);

        return JsonSerializer.Deserialize<MarketingPreferenceTokenPayload>(json)
               ?? throw new InvalidOperationException("Invalid preference token.");
    }
}
