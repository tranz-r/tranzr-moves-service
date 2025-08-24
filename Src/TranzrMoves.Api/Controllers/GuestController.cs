using ErrorOr;
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
public class GuestController(
    IQuoteRepository quoteRepository,
    IUserRepository userRepository,
    IUserQuoteRepository userQuoteRepository,
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
                
                var updatedQuote = await quoteRepository.UpsertQuoteAsync(guestId, quoteEntity, body.ETag, ct);
                if (updatedQuote is null)
                {
                    logger.LogInformation("ETag mismatch for guest {GuestId}", guestId);
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }

                // Handle customer data if provided
                if (body.Customer != null)
                {
                    try
                    {
                        User? existingUser = null;
                        User? userToSave = null;
                        
                        // Check if user already exists by email
                        if (!string.IsNullOrEmpty(body.Customer.Email))
                        {
                            existingUser = await userRepository.GetUserByEmailAsync(body.Customer.Email, ct);
                        }

                        if (existingUser != null)
                        {
                            // Update existing user
                            existingUser.FullName = body.Customer.FullName ?? existingUser.FullName;
                            existingUser.PhoneNumber = body.Customer.PhoneNumber ?? existingUser.PhoneNumber;
                            
                            // Update billing address if provided
                            if (body.Customer.BillingAddress != null)
                            {
                                if (existingUser.BillingAddress == null)
                                {
                                    existingUser.BillingAddress = new Address
                                    {
                                        Line1 = body.Customer.BillingAddress.Line1,
                                        PostCode = body.Customer.BillingAddress.PostCode
                                    };
                                }
                                
                                existingUser.BillingAddress.Line1 = body.Customer.BillingAddress.Line1;
                                existingUser.BillingAddress.Line2 = body.Customer.BillingAddress.Line2;
                                existingUser.BillingAddress.City = body.Customer.BillingAddress.City;
                                existingUser.BillingAddress.County = body.Customer.BillingAddress.County;
                                existingUser.BillingAddress.PostCode = body.Customer.BillingAddress.PostCode;
                                existingUser.BillingAddress.Country = body.Customer.BillingAddress.Country;
                                existingUser.BillingAddress.HasElevator = body.Customer.BillingAddress.HasElevator;
                                existingUser.BillingAddress.Floor = body.Customer.BillingAddress.Floor;
                            }
                            
                            var updateResult = await userRepository.UpdateUserAsync(existingUser, ct);
                            if (updateResult.IsError)
                            {
                                logger.LogWarning("Failed to update existing user {UserId}: {Error}", 
                                    existingUser.Id, updateResult.FirstError.Description);
                            }
                            else
                            {
                                userToSave = updateResult.Value;
                            }
                        }
                        else
                        {
                            // Create new user
                            var newUser = new User
                            {
                                FullName = body.Customer.FullName,
                                Email = body.Customer.Email,
                                PhoneNumber = body.Customer.PhoneNumber,
                                BillingAddress = body.Customer.BillingAddress != null ? new Address
                                {
                                    Line1 = body.Customer.BillingAddress.Line1,
                                    Line2 = body.Customer.BillingAddress.Line2,
                                    City = body.Customer.BillingAddress.City,
                                    County = body.Customer.BillingAddress.County,
                                    PostCode = body.Customer.BillingAddress.PostCode,
                                    Country = body.Customer.BillingAddress.Country,
                                    HasElevator = body.Customer.BillingAddress.HasElevator,
                                    Floor = body.Customer.BillingAddress.Floor
                                } : null
                            };
                            
                            var createResult = await userRepository.AddUserAsync(newUser, ct);
                            if (createResult.IsError)
                            {
                                logger.LogWarning("Failed to create new user: {Error}", createResult.FirstError.Description);
                            }
                            else
                            {
                                userToSave = createResult.Value;
                            }
                        }

                        // Create CustomerQuote relationship if user was successfully saved
                        if (userToSave != null)
                        {
                            var customerQuote = new CustomerQuote
                            {
                                UserId = userToSave.Id,
                                QuoteId = updatedQuote.Id
                            };
                            
                            var relationshipResult = await userQuoteRepository.AddUserQuoteAsync(customerQuote, ct);
                            if (relationshipResult.IsError && relationshipResult.FirstError.Type == ErrorType.Conflict)
                            {
                                logger.LogWarning("User quote relationship already exists: {Error}", 
                                    relationshipResult.FirstError.Description);
                            }
                            else
                            {
                                logger.LogInformation("Successfully created CustomerQuote relationship for user {UserId} and quote {QuoteId}", 
                                    userToSave.Id, quoteEntity.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error handling customer data for quote {QuoteId}, continuing without customer data", quoteEntity.Id);
                    }
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



public record SelectQuoteTypeRequest
{
    public string QuoteType { get; set; } = string.Empty;
}
