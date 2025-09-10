using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Quote.SelectQuoteType;
using TranzrMoves.Application.Features.Quote.Save;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Application.Mapper;

namespace TranzrMoves.Api.Controllers;

/// <summary>
/// Guest Controller for quote management operations.
/// 
/// ETag Pattern:
/// - All endpoints that return quote data include ETag headers for consistency
/// - ETags are derived from the quote's Version property (PostgreSQL xmin)
/// - GET endpoints use ETags for HTTP caching (If-None-Match → 304)
/// - POST endpoints use ETags for concurrency control (If-Match → 412)
/// - Response body ETags are maintained for backward compatibility
/// </summary>
[ApiController]
[Route("api/guest")]
public class GuestController(
    IQuoteRepository quoteRepository,
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

    [HttpGet("quote")]
    public async Task<IActionResult> GetQuote([FromQuery(Name = "quoteType")] QuoteType quoteType, CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Ok(new { quote = (object?)null, etag = (string?)null });
        }

        var quote = await quoteRepository.GetQuoteAsync(guestId, quoteType, ct);
        if (quote is null)
        {
            return Ok(new { quote = (object?)null, etag = (string?)null });
        }

        // Use the quote's Version (xmin) as the ETag for concurrency control
        var quoteEtag = quote.Version.ToString();

        // Check If-None-Match
        var ifNoneMatch = Request.Headers["If-None-Match"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, quoteEtag, StringComparison.Ordinal))
        {
            Response.Headers.ETag = quoteEtag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = quoteEtag;
        RefreshCookie(guestId);
        
        var mapper = new QuoteMapper();
        var quoteDto = mapper.ToDto(quote);
        
        var quoteTypeDto =  new QuoteTypeDto
        {
            Quote = quoteDto,
            Etag = quoteEtag
        };
        
        return Ok(quoteTypeDto);
    }

    [HttpPost("quote")]
    public async Task<IActionResult> SaveQuote([FromBody] SaveQuoteRequest? body, CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId)) return Unauthorized();

        if (body is null)
        {
            return BadRequest("Request body is required");
        }

        var command = new SaveQuoteCommand(body.Quote, body.Customer, body.ETag);
        var result = await mediator.Send(command, ct);
        
        if (result.IsError)
        {
            logger.LogError("Failed to save quote: {Error}", result.FirstError.Description);
            return Problem(result.Errors.ToList());
        }
        
        // Set ETag header for consistency with other endpoints
        Response.Headers.ETag = result.Value.ETag;
        
        RefreshCookie(guestId);
        
        return Ok(new { etag = result.Value.ETag });
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
    public async Task<IActionResult> SelectQuoteType([FromQuery(Name = "quoteType")] QuoteType quoteType, CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized();
        }

        var command = new SelectQuoteTypeCommand(guestId, quoteType);
        var result = await mediator.Send(command, ct);
        
        if (result.IsError)
        {
            logger.LogError("Failed to select quote type: {Error}", result.FirstError.Description);
            return Problem(result.Errors.ToList());
        }
        
        // Use the quote's Version (xmin) as the ETag for concurrency control
        var etag = result.Value.Version.ToString();
        
        // Add ETag header for consistency with other endpoints
        Response.Headers.ETag = etag;
        
        RefreshCookie(guestId);
        
        var quoteTypeDto =  new QuoteTypeDto
        {
            Quote = result.Value,
            Etag = etag
        };
        
        return Ok(quoteTypeDto);
    }

    [HttpGet("customer/{quoteId}")]
    public async Task<IActionResult> GetCustomerData(string quoteId, CancellationToken ct)
    {
        if (!Guid.TryParse(quoteId, out var quoteGuid))
        {
            return BadRequest("Invalid quote ID format");
        }
        
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId)) return Unauthorized();

        try
        {
            // Get the user repository and user quote repository from DI
            var userRepository = HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var userQuoteRepository = HttpContext.RequestServices.GetRequiredService<IUserQuoteRepository>();

            // Find the CustomerQuote relationship for this quote
            var customerQuote = await userQuoteRepository.GetUserQuoteByQuoteIdAsync(quoteGuid, ct);
            if (customerQuote == null)
            {
                logger.LogInformation("No customer found for quote {QuoteId}", quoteId);
                return NotFound("Customer data not found for this quote");
            }

            // Get the user data
            var user = await userRepository.GetUserAsync(customerQuote.UserId, ct);
            if (user == null)
            {
                logger.LogWarning("User not found for customer quote {CustomerQuoteId}", customerQuote.Id);
                return NotFound("User data not found");
            }

            // Map to DTO format that frontend expects
            var customerData = new
            {
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                role = user.Role.ToString(),
                billingAddress = user.BillingAddress != null ? new
                {
                    id = user.BillingAddress.Id,
                    userId = user.BillingAddress.UserId,
                    line1 = user.BillingAddress.Line1,
                    line2 = user.BillingAddress.Line2,
                    city = user.BillingAddress.City,
                    county = user.BillingAddress.County,
                    postCode = user.BillingAddress.PostCode,
                    country = user.BillingAddress.Country,
                    hasElevator = user.BillingAddress.HasElevator,
                    floor = user.BillingAddress.Floor
                } : null
            };

            RefreshCookie(guestId);
            return Ok(customerData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving customer data for quote {QuoteId}", quoteId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve customer data");
        }
    }

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
