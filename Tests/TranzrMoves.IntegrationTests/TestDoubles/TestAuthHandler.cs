using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TranzrMoves.IntegrationTests.TestDoubles;

public static class TestAuthDefaults
{
    public const string Scheme = "Test";
    public const string SupabaseIdHeader = "X-Test-Supabase-Id";
    public const string EmailHeader = "X-Test-Email";
}

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestAuthDefaults.SupabaseIdHeader, out var supabaseIdValues)
            || string.IsNullOrWhiteSpace(supabaseIdValues.FirstOrDefault()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var supabaseId = supabaseIdValues.First()!;
        var email = Request.Headers.TryGetValue(TestAuthDefaults.EmailHeader, out var emailValues)
            ? emailValues.FirstOrDefault() ?? "test@example.com"
            : "test@example.com";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, supabaseId),
            new Claim("email", email),
        };

        var identity = new ClaimsIdentity(claims, TestAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestAuthDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
