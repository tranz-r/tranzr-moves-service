using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.LegalDocuments.Create;
using TranzrMoves.Application.Features.LegalDocuments.Get;
using TranzrMoves.Application.Features.LegalDocuments.GetHistory;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class LegalController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("terms-and-conditions")]
    public async Task<IActionResult> CreateTermsAndConditions(
        [FromBody] CreateLegalDocumentRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new CreateLegalDocumentCommand(request with { DocumentType = LegalDocumentType.TermsAndConditions });
        var result = await mediator.Send(command, cancellationToken);
        return result.Match(
            response => CreatedAtAction(nameof(GetTermsAndConditions), new { }, response),
            Problem);
    }

    [HttpGet("terms-and-conditions")]
    public async Task<IActionResult> GetTermsAndConditions(
        [FromQuery] DateTimeOffset? asOfDate, 
        CancellationToken cancellationToken)
    {
        var request = new GetLegalDocumentRequest(LegalDocumentType.TermsAndConditions, asOfDate);
        var query = new GetLegalDocumentQuery(request);
        var result = await mediator.Send(query, cancellationToken);
        return result.Match(
            response => {
                // Check for conditional requests
                var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault();
                var ifModifiedSince = Request.Headers.IfModifiedSince.FirstOrDefault();
                
                // If client has the same version, return 304 Not Modified
                if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == $"\"{response.Version}\"")
                {
                    return StatusCode(304); // Not Modified
                }
                
                // If client's cached version is newer or same, return 304 Not Modified
                if (!string.IsNullOrEmpty(ifModifiedSince) && 
                    DateTime.TryParse(ifModifiedSince, out var clientModified) &&
                    clientModified >= response.CreatedAt)
                {
                    return StatusCode(304); // Not Modified
                }
                
                // Set intelligent cache control headers for legal documents
                // Cache until the next document becomes effective (when EffectiveTo is set),
                // or 24 hours minimum for current documents (EffectiveTo is null)
                var cacheUntil = response.EffectiveTo ?? DateTimeOffset.UtcNow.AddDays(1);
                var maxAge = Math.Max(86400, (int)(cacheUntil - DateTimeOffset.UtcNow).TotalSeconds);
                Response.Headers.CacheControl = $"public, max-age={maxAge}"; // Cache until next effective date
                Response.Headers.ETag = $"\"{response.Version}\""; // Use document version as ETag
                Response.Headers.LastModified = response.CreatedAt.ToString("R"); // RFC 1123 format
                return Ok(response);
            },
            Problem);
    }

    [HttpGet("terms-and-conditions/history")]
    public async Task<IActionResult> GetTermsAndConditionsHistory(CancellationToken cancellationToken)
    {
        var query = new GetLegalDocumentHistoryQuery(LegalDocumentType.TermsAndConditions);
        var result = await mediator.Send(query, cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("privacy-policy")]
    public async Task<IActionResult> CreatePrivacyPolicy(
        [FromBody] CreateLegalDocumentRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new CreateLegalDocumentCommand(request with { DocumentType = LegalDocumentType.PrivacyPolicy });
        var result = await mediator.Send(command, cancellationToken);
        return result.Match(
            response => CreatedAtAction(nameof(GetPrivacyPolicy), new { }, response),
            Problem);
    }

    [HttpGet("privacy-policy")]
    public async Task<IActionResult> GetPrivacyPolicy(
        [FromQuery] DateTimeOffset? asOfDate, 
        CancellationToken cancellationToken)
    {
        var request = new GetLegalDocumentRequest(LegalDocumentType.PrivacyPolicy, asOfDate);
        var query = new GetLegalDocumentQuery(request);
        var result = await mediator.Send(query, cancellationToken);
        return result.Match(
            response => {
                // Check for conditional requests
                var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault();
                var ifModifiedSince = Request.Headers.IfModifiedSince.FirstOrDefault();
                
                // If client has the same version, return 304 Not Modified
                if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == $"\"{response.Version}\"")
                {
                    return StatusCode(304); // Not Modified
                }
                
                // If client's cached version is newer or same, return 304 Not Modified
                if (!string.IsNullOrEmpty(ifModifiedSince) && 
                    DateTime.TryParse(ifModifiedSince, out var clientModified) &&
                    clientModified >= response.CreatedAt)
                {
                    return StatusCode(304); // Not Modified
                }
                
                // Set intelligent cache control headers for legal documents
                // Cache until the next document becomes effective (when EffectiveTo is set),
                // or 24 hours minimum for current documents (EffectiveTo is null)
                var cacheUntil = response.EffectiveTo ?? DateTimeOffset.UtcNow.AddDays(1);
                var maxAge = Math.Max(86400, (int)(cacheUntil - DateTimeOffset.UtcNow).TotalSeconds);
                Response.Headers.CacheControl = $"public, max-age={maxAge}"; // Cache until next effective date
                Response.Headers.ETag = $"\"{response.Version}\""; // Use document version as ETag
                Response.Headers.LastModified = response.CreatedAt.ToString("R"); // RFC 1123 format
                return Ok(response);
            },
            Problem);
    }

    [HttpGet("privacy-policy/history")]
    public async Task<IActionResult> GetPrivacyPolicyHistory(CancellationToken cancellationToken)
    {
        var query = new GetLegalDocumentHistoryQuery(LegalDocumentType.PrivacyPolicy);
        var result = await mediator.Send(query, cancellationToken);
        return result.Match(Ok, Problem);
    }
}
