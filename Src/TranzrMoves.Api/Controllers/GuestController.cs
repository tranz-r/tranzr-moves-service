using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Quote.SelectQuoteType;
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
        
        var existingQuote = await quoteRepository.GetQuoteAsync(guestId, body.Quote.Type.Value, ct);
        
        if (existingQuote is null)
        {
            logger.LogWarning("No existing quote found for guest {GuestId} and type {QuoteType}", guestId, body.Quote.Type.Value);
            return NotFound("Quote not found");
        }

        // Handle new entity-based save
        try
        {
            var mapper = new QuoteMapper();
            mapper.UpdateEntity(body.Quote, existingQuote);
            
            var result = await quoteRepository.UpdateQuoteAsync(guestId, existingQuote, ct);
            
            if (result.IsError)
            {
                if (result.FirstError.Type == ErrorType.Conflict)
                {
                    logger.LogInformation("ETag mismatch for guest {GuestId}", guestId);
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
                }
                
                logger.LogWarning("Failed to update quote for guest {GuestId}: {Error}", guestId, result.FirstError.Description);
                return BadRequest(result.Errors.ToList());
            }
            
            var updatedQuote = result.Value;

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
                                userToSave.Id, updatedQuote.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error handling customer data for quote {QuoteId}, continuing without customer data", updatedQuote.Id);
                }
            }

            // Return the updated quote's Version as the new ETag
            var newEtag = updatedQuote.Version.ToString();
            Response.Headers.ETag = newEtag;
            return Ok(new { etag = newEtag });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error mapping QuoteDto to Quote entity for guest {GuestId}", guestId);
            return BadRequest("Invalid quote data format");
        }
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

    // 🔍 TEST ENDPOINT: Test MapperIgnoreTarget behavior
    [HttpPost("test-mapper")]
    public async Task<IActionResult> TestMapper(CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized();
        }

        try
        {
            // Get an existing quote
            var existingQuote = await quoteRepository.GetQuoteAsync(guestId, QuoteType.Send, ct);
            if (existingQuote == null)
            {
                return NotFound("No quote found to test with");
            }

            logger.LogInformation("🔍 MAPPER TEST - Original Quote: ID={Id}, Version={Version}, Reference={Reference}", 
                existingQuote.Id, existingQuote.Version, existingQuote.QuoteReference);

            // Create a test DTO with a different version
            var testDto = new QuoteDto
            {
                Version = 99999, // Different version to test if it gets ignored
                Type = QuoteType.Send,
                VanType = Domain.Entities.VanType.mediumVan, // Different value to test mapping
                DriverCount = 3
            };

            // Test the mapper
            var mapper = new QuoteMapper();
            mapper.UpdateEntity(testDto, existingQuote);

            logger.LogInformation("🔍 MAPPER TEST - After mapping: ID={Id}, Version={Version}, VanType={VanType}, DriverCount={DriverCount}", 
                existingQuote.Id, existingQuote.Version, existingQuote.VanType, existingQuote.DriverCount);

            // Check if Version was preserved
            if (existingQuote.Version == 99999)
            {
                logger.LogWarning("⚠️ MAPPER TEST FAILED: Version was overwritten! MapperIgnoreTarget not working");
                return BadRequest(new { 
                    success = false, 
                    message = "MapperIgnoreTarget failed - Version was overwritten",
                    originalVersion = existingQuote.Version,
                    expectedBehavior = "Version should be preserved"
                });
            }
            else
            {
                logger.LogInformation("✅ MAPPER TEST PASSED: Version preserved. MapperIgnoreTarget working correctly");
                return Ok(new { 
                    success = true, 
                    message = "MapperIgnoreTarget working correctly",
                    originalVersion = existingQuote.Version,
                    vanTypeUpdated = existingQuote.VanType == Domain.Entities.VanType.mediumVan,
                    driverCountUpdated = existingQuote.DriverCount == 3
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing mapper");
            return StatusCode(500, new { error = ex.Message });
        }
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
