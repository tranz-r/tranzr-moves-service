using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Notifications.Application.Services;
using TranzrMoves.Notifications.Contracts;

namespace TranzrMoves.Api.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("api/v{v:apiVersion}/marketing-preferences")]
public sealed class MarketingPreferencesController(
    IMarketingPreferenceTokenService tokenService,
    IMarketingPreferenceService preferenceService) : ApiControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        OperationId = "MarketingPreferences_Get",
        Summary = "Load marketing preferences using a signed token",
        Tags = new[] { "Marketing preferences (v2)" })]
    [ProducesResponseType(typeof(MarketingPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAsync([FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Token is required.");
        }

        try
        {
            var payload = tokenService.Read(token);
            var preferences = await preferenceService.GetByIdAsync(payload.PrefId, cancellationToken);
            if (preferences is null || !string.Equals(preferences.Email, payload.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid or expired preference link.");
            }

            return Ok(ToResponse(preferences));
        }
        catch (Exception)
        {
            return BadRequest("Invalid or expired preference link.");
        }
    }

    [HttpPut]
    [SwaggerOperation(
        OperationId = "MarketingPreferences_Update",
        Summary = "Update marketing preferences from the preference centre",
        Tags = new[] { "Marketing preferences (v2)" })]
    [ProducesResponseType(typeof(MarketingPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(
        [FromHeader(Name = "X-Preference-Token")] string? preferenceToken,
        [FromBody] UpdateMarketingPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(preferenceToken))
        {
            return BadRequest("X-Preference-Token header is required.");
        }

        try
        {
            var payload = tokenService.Read(preferenceToken);
            var preferences = await preferenceService.ApplyPreferencesAsync(
                new ApplyMarketingPreferencesRequest(
                    payload.Email,
                    request.EmailMarketingEnabled,
                    request.SmsMarketingEnabled,
                    MarketingConsentSource.PreferenceCentre,
                    CustomerId: null,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString()),
                cancellationToken);

            return Ok(ToResponse(preferences));
        }
        catch (Exception)
        {
            return BadRequest("Invalid or expired preference link.");
        }
    }

    [HttpGet("unsubscribe/email")]
    [SwaggerOperation(
        OperationId = "MarketingPreferences_UnsubscribeEmail",
        Summary = "One-click email marketing unsubscribe",
        Tags = new[] { "Marketing preferences (v2)" })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnsubscribeEmailAsync([FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Token is required.");
        }

        try
        {
            var payload = tokenService.Read(token);
            var current = await preferenceService.GetByIdAsync(payload.PrefId, cancellationToken);
            if (current is null || !string.Equals(current.Email, payload.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid or expired unsubscribe link.");
            }

            await preferenceService.ApplyPreferencesAsync(
                new ApplyMarketingPreferencesRequest(
                    payload.Email,
                    EmailMarketingEnabled: false,
                    SmsMarketingEnabled: current.SmsMarketingEnabled,
                    MarketingConsentSource.PreferenceCentre,
                    CustomerId: null,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString()),
                cancellationToken);

            return Content(
                "<html><body><h1>Unsubscribed</h1><p>You will no longer receive marketing emails from Tranzr Moves.</p></body></html>",
                "text/html");
        }
        catch (Exception)
        {
            return BadRequest("Invalid or expired unsubscribe link.");
        }
    }

    private static MarketingPreferencesResponse ToResponse(MarketingPreferenceDto preferences) =>
        new()
        {
            EmailMarketingEnabled = preferences.EmailMarketingEnabled,
            SmsMarketingEnabled = preferences.SmsMarketingEnabled
        };
}
