using System.Text.Json;
using System.Text.Json.Serialization;

using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public class TurnstileService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<TurnstileService> logger) : ITurnstileService
{
    private readonly string _secretKey = configuration["TURNSTILE_SECRET_KEY"] ??
        throw new InvalidOperationException("TURNSTILE_SECRET_KEY is not configured");

    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<ErrorOr<bool>> ValidateTokenAsync(string token, string? remoteIp = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("Turnstile token is empty or null");
                return Error.Validation("Turnstile.Validation", "Turnstile token is required");
            }

            var formData = new List<KeyValuePair<string, string>>
            {
                new("secret", _secretKey),
                new("response", token)
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                formData.Add(new KeyValuePair<string, string>("remoteip", remoteIp));
            }

            var content = new FormUrlEncodedContent(formData);

            logger.LogInformation("Validating Turnstile token with Cloudflare");

            var response = await httpClient.PostAsync(SiteVerifyUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<TurnstileResponse>(responseContent);

            if (result == null)
            {
                logger.LogError("Failed to deserialize Turnstile response");
                return Error.Failure("Turnstile.Deserialization", "Failed to validate Turnstile token");
            }

            if (!result.Success)
            {
                logger.LogWarning("Turnstile validation failed. Error codes: {ErrorCodes}",
                    string.Join(", ", result.ErrorCodes ?? new List<string>()));
                return Error.Validation("Turnstile.Validation", "Turnstile validation failed");
            }

            logger.LogInformation("Turnstile token validated successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating Turnstile token");
            return Error.Failure("Turnstile.Validation", "Failed to validate Turnstile token");
        }
    }

    public class Metadata
    {
        [JsonPropertyName("interactive")]
        public bool Interactive { get; set; }
    }

    public class TurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string ChallengeTs { get; set; } = string.Empty;

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = string.Empty;

        [JsonPropertyName("error-codes")]
        public List<string> ErrorCodes { get; set; } = new();

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("cdata")]
        public string Cdata { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Metadata? Metadata { get; set; }
    }
}
