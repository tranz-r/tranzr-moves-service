using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Quote.Create;
using TranzrMoves.Application.Features.Quote.SelectQuoteType;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Application.Mapper;

namespace TranzrMoves.Api.Controllers;

[ApiController]
[Route("api/guest")]
public class GuestController(IQuoteRepository quoteRepository, 
    IMediator mediator,
    ILogger<GuestController> logger) : ApiControllerBase
{
    private const string CookieName = "tranzr_guest";

    [HttpPost("ensure")]
    public async Task<IActionResult> Ensure(CancellationToken ct)
    {
        var cookie = Request.Cookies[CookieName];
        string guestId;

        if (string.IsNullOrWhiteSpace(cookie))
        {
            guestId = Guid.NewGuid().ToString();
        }
        else
        {
            guestId = cookie;
        }

        // Refresh expiration each call
        Response.Cookies.Append(CookieName, guestId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(60)
        });

        await quoteRepository.CreateIfMissingAsync(guestId, ct);
        return Ok(new { guestId });
    }

    [HttpGet("session")]
    public async Task<IActionResult> GetSession(CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Ok(new { state = (QuoteContextDto?)null, etag = (string?)null });
        }

        var state = await quoteRepository.GetQuoteContextStateAsync(guestId, ct);
        if (state is null)
        {
            return Ok(new { state = (QuoteContextDto?)null, etag = (string?)null });
        }

        var session = await quoteRepository.GetSessionAsync(guestId, ct);
        var etag = session?.ETag ?? string.Empty;

        // Check If-None-Match
        var ifNoneMatch = Request.Headers["If-None-Match"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, etag, StringComparison.Ordinal))
        {
            Response.Headers.ETag = etag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;
        RefreshCookie(guestId);
        return Ok(new { state, etag });
    }

    [HttpPost("session")]
    public async Task<IActionResult> SaveSession([FromBody] QuoteContextDto dto, [FromHeader(Name = "If-Match")] string? etag, CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        
        var response = await mediator.Send(new CreateQuotesCommand(dto, guestId, etag), ct);
        if (!string.IsNullOrEmpty(response.Value.Etag)) Response.Headers.ETag = response.Value.Etag;
        
        RefreshCookie(guestId);
        return Ok(new { etag = response.Value.Etag });
    }

    [HttpGet("quote")]
    public async Task<IActionResult> GetQuote([FromQuery] string? type, CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Ok(new { quote = (object?)null, etag = (string?)null });
        }

        // If type is specified, get specific quote
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<QuoteType>(type, true, out var quoteType))
        {
            var quote = await quoteRepository.GetQuoteAsync(guestId, quoteType, ct);
            if (quote is null)
            {
                return Ok(new { quote = (object?)null, etag = (string?)null });
            }

            var quoteSession = await quoteRepository.GetSessionAsync(guestId, ct);
            var quoteEtag = quoteSession?.ETag ?? string.Empty;

            // Check If-None-Match
            var ifNoneMatch = Request.Headers["If-None-Match"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, quoteEtag, StringComparison.Ordinal))
            {
                Response.Headers.ETag = quoteEtag;
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = quoteEtag;
            RefreshCookie(guestId);
            return Ok(new { quote, etag = quoteEtag });
        }

        // Get all quotes for session
        var quotes = await quoteRepository.GetQuotesForSessionAsync(guestId, ct);
        var session = await quoteRepository.GetSessionAsync(guestId, ct);
        
        if (quotes.Count == 0)
        {
            return Ok(new { quotes = Array.Empty<object>(), etag = (string?)null });
        }

        var etag = session?.ETag ?? string.Empty;

        // Check If-None-Match using session ETag
        var ifNoneMatchSession = Request.Headers["If-None-Match"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ifNoneMatchSession) && string.Equals(ifNoneMatchSession, etag, StringComparison.Ordinal))
        {
            Response.Headers.ETag = etag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;
        RefreshCookie(guestId);
        return Ok(new { quotes, etag });
    }

    [HttpPost("quote")]
    public async Task<IActionResult> SaveQuote([FromBody] SaveQuoteRequest body, CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId)) return Unauthorized();

        if (body is null) return BadRequest("Request body is required");

        // Handle new entity-based save
        if (body.Quote is not null)
        {
            try
            {
                // Map QuoteDto to Quote entity using Mapperly
                var quoteMapper = new QuoteMapper();
                var quoteEntity = quoteMapper.ToEntity(body.Quote);
                
                // Set the session ID from the guest ID
                quoteEntity.SessionId = guestId;
                
                var success = await quoteRepository.UpsertQuoteAsync(guestId, quoteEntity, body.ETag, ct);
                if (!success)
                {
                    logger.LogInformation("ETag mismatch for guest {GuestId}", guestId);
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }

                // Get the session to retrieve the updated ETag
                var updatedSession = await quoteRepository.GetSessionAsync(guestId, ct);
                var newEtag = updatedSession?.ETag;
                if (!string.IsNullOrEmpty(newEtag)) Response.Headers.ETag = newEtag;
                return Ok(new { etag = newEtag });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error mapping QuoteDto to Quote entity for guest {GuestId}", guestId);
                return BadRequest("Invalid quote data format");
            }
        }

        return BadRequest("Quote must be provided");
    }

    [HttpDelete("quote")]
    public async Task<IActionResult> DeleteQuote([FromQuery] string type, CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId)) return Unauthorized();

        if (!Enum.TryParse<QuoteType>(type, true, out var quoteType))
        {
            return BadRequest("Invalid quote type");
        }

        var success = await quoteRepository.DeleteQuoteAsync(guestId, quoteType, ct);
        if (!success)
        {
            return NotFound("Quote not found");
        }

        return Ok(new { deleted = true });
    }

    [HttpPost("select-quote-type")]
    public async Task<IActionResult> SelectQuoteType([FromBody] SelectQuoteTypeRequest request, CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized();
        }

        if (!Enum.TryParse<QuoteType>(request.QuoteType, true, out var quoteType))
        {
            return BadRequest("Invalid quote type");
        }

        var command = new SelectQuoteTypeCommand(guestId, quoteType);
        var result = await mediator.Send(command, ct);
        
        if (result.IsError)
        {
            logger.LogError("Failed to select quote type: {Error}", result.FirstError.Description);
            return Problem(result.Errors.ToList());
        }

        var response = result.Value;
        
        // Get updated session for ETag
        var session = await quoteRepository.GetSessionAsync(guestId, ct);
        var etag = session?.ETag ?? string.Empty;
        
        RefreshCookie(guestId);
        return Ok(new { 
            quote = new {
                id = response.Id,
                type = response.Type,
                quoteReference = response.QuoteReference,
                sessionId = response.SessionId
            }, 
            etag 
        });
    }

    // [HttpPost("shared-data")]
    // public async Task<IActionResult> UpdateSharedData([FromBody] SharedData sharedData, CancellationToken ct)
    // {
    //     var guestId = Request.Cookies[CookieName];
    //     if (string.IsNullOrWhiteSpace(guestId)) return Unauthorized();
    //
    //     if (sharedData is null) return BadRequest("Request body is required");
    //
    //     // Get current ETag from request headers
    //     var etag = Request.Headers["If-Match"].FirstOrDefault();
    //
    //     var success = await quoteStore.UpdateSharedDataAsync(guestId, sharedData, etag, ct);
    //     if (!success)
    //     {
    //         logger.LogInformation("ETag mismatch for guest {GuestId}", guestId);
    //         return StatusCode(StatusCodes.Status412PreconditionFailed);
    //     }
    //
    //     // Get updated session to return new ETag
    //     var updatedSession = await quoteStore.GetSessionAsync(guestId, ct);
    //     var newEtag = updatedSession?.ETag;
    //     if (!string.IsNullOrEmpty(newEtag)) Response.Headers.ETag = newEtag;
    //     
    //     RefreshCookie(guestId);
    //     return Ok(new { etag = newEtag });
    // }

    [HttpPost("cleanup-expired")]
    public async Task<IActionResult> CleanupExpired(CancellationToken ct)
    {
        // Clean up expired sessions (which will cascade to quotes)
        var db = HttpContext.RequestServices.GetRequiredService<Infrastructure.TranzrMovesDbContext>();
        var now = DateTimeOffset.UtcNow;
            
        var expiredSessions = await db.Set<QuoteSession>()
            .Where(q => q.ExpiresUtc != null && q.ExpiresUtc < now)
            .ExecuteDeleteAsync(ct);
            
        logger.LogInformation("Cleaned up {SessionCount} expired sessions", expiredSessions);
            
        return Ok(new { expiredSessions });
    }

    private void RefreshCookie(string guestId)
    {
        Response.Cookies.Append(CookieName, guestId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(60)
        });
    }
}



public record SelectQuoteTypeRequest
{
    public string QuoteType { get; set; } = string.Empty;
}
