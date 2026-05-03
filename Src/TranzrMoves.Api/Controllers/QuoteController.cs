using System.Globalization;
using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Swashbuckle.AspNetCore.Annotations;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Admin.Quote.Details;
using TranzrMoves.Application.Features.Admin.Quote.Driver;
using TranzrMoves.Application.Features.Admin.Quote.List;
using TranzrMoves.Application.Features.Admin.Quote.Notes;
using TranzrMoves.Application.Features.Admin.Quote.Status;
using TranzrMoves.Application.Features.Quote.Journey.Init;
using TranzrMoves.Application.Features.Quote.Journey.State;
using TranzrMoves.Application.Features.Quote.Patch.Addresses;
using TranzrMoves.Application.Features.Quote.Patch.CustomerInfo;
using TranzrMoves.Application.Features.Quote.Patch.EmailAndPhoneNumber;
using TranzrMoves.Application.Features.Quote.Patch.Inventory;
using TranzrMoves.Application.Features.Quote.Patch.MoveDateTime;
using TranzrMoves.Application.Features.Quote.Patch.Pricing;
using TranzrMoves.Application.Features.Quote.Patch.Summary;
using TranzrMoves.Application.Features.Quote.SelectQuoteType;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Api.Controllers;

/// <summary>
/// Guest Controller for quote management operations.
///
/// ETag pattern:
/// - V1 guest quote GET uses ETag / If-None-Match as documented on that action.
/// - V2 <see cref="QuoteJourneyResponse"/> endpoints expose <c>quote.version</c> in the body and the same value in the <c>ETag</c> response header (PostgreSQL xmin).
/// - V2 GET <c>journey-state</c> supports <c>If-None-Match</c> → 304.
/// - V2 PATCH steps require <c>If-Match</c> with that version; mismatches or concurrent saves return 412 via <see cref="QuoteV2Errors.ConcurrencyConflictCode"/>.
/// </summary>

[ApiVersion(1)]
[ApiVersion(2)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class QuoteController(
    IQuoteRepository quoteRepository,
    IMediator mediator,
    ITimeService timeService,
    IQuoteResumeResolver quoteResumeResolver,
    IQuoteResumeTokenService quoteResumeTokenService,
    ILogger<QuoteController> logger) : ApiControllerBase
{
    private new const string CookieName = "tranzr_guest";

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
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(60)
        });

        await quoteRepository.CreateIfMissingAsync(guestId, ct);
        return Ok(new { guestId });
    }

    [HttpGet]
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

        // Check If-None-Match (strip weak prefix / quotes — clients send RFC 7232 forms)
        var ifNoneMatch = Request.Headers["If-None-Match"].FirstOrDefault();
        if (EntityTagsMatch(ifNoneMatch, quoteEtag))
        {
            Response.Headers.ETag = quoteEtag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = quoteEtag;
        RefreshCookie(guestId);

        var mapper = new QuoteMapper();
        var quoteDto = mapper.ToDto(quote);

        var quoteTypeDto = new QuoteTypeDto
        {
            Quote = quoteDto,
            Etag = quoteEtag
        };

        return Ok(quoteTypeDto);
    }

    [HttpGet("checkout-session")]
    public async Task<IActionResult> GetQuote([FromQuery(Name = "session_id")] string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest("session_id is required");
        }

        var quote = await quoteRepository.GetQuoteByStripeCheckoutSessionIdAsync(sessionId, ct);

        if (quote is null)
        {
            return NotFound("Quote not found for the given session ID");
        }

        var mapper = new QuoteMapper();
        var quoteDto = mapper.ToDto(quote);

        return Ok(quoteDto);
    }

    [HttpDelete]
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

        var quoteTypeDto = new QuoteTypeDto
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

    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminQuotes(
        [FromQuery] bool admin,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDir = "desc",
        [FromQuery] string? status = null,
        [FromQuery] LocalDate? dateFrom = null,
        [FromQuery] LocalDate? dateTo = null,
        CancellationToken ct = default)
    {
        if (!admin)
        {
            return BadRequest("Admin flag is required for this endpoint");
        }

        try
        {
            var query = new AdminQuoteListQuery(
                page,
                pageSize,
                search,
                sortBy,
                sortDir,
                status,
                dateFrom,
                dateTo);

            var result = await mediator.Send(query, ct);

            if (result.IsError)
            {
                logger.LogError("Failed to retrieve admin quotes: {Error}", result.FirstError.Description);
                return Problem(result.Errors.ToList());
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving admin quotes");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve quotes");
        }
    }

    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetQuoteDetails(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var query = new AdminQuoteDetailsQuery(id);
            var result = await mediator.Send(query, ct);

            if (result.IsError)
            {
                logger.LogError("Failed to retrieve quote details: {Error}", result.FirstError.Description);
                return Problem(result.Errors.ToList());
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving quote details");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve quote details");
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateQuoteStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateQuoteStatusRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var command = new UpdateQuoteStatusCommand(
                id,
                request.Status,
                request.Reason,
                request.AdminNote,
                request.NotifyCustomer,
                request.NotifyDriver);

            var result = await mediator.Send(command, ct);

            if (result.IsError)
            {
                logger.LogError("Failed to update quote status: {Error}", result.FirstError.Description);
                return Problem(result.Errors.ToList());
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating quote status");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update quote status");
        }
    }

    [HttpPost("{id}/driver-assign/{driverId}")]
    public async Task<IActionResult> AssignDriver(
        [FromRoute] Guid id,
        [FromRoute] Guid driverId,
        [FromBody] AssignDriverRequest? request = null,
        CancellationToken ct = default)
    {
        try
        {
            var command = new AssignDriverCommand(
                id,
                driverId,
                request?.Reason,
                request?.AdminNote,
                request?.NotifyDriver ?? false,
                request?.NotifyCustomer ?? false);

            var result = await mediator.Send(command, ct);

            if (result.IsError)
            {
                logger.LogError("Failed to assign driver: {Error}", result.FirstError.Description);
                return Problem(result.Errors.ToList());
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning driver");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to assign driver");
        }
    }

    [HttpDelete("{id}/driver-unassign/{driverId}")]
    public async Task<IActionResult> UnassignDriver(
        [FromRoute] Guid id,
        [FromRoute] Guid driverId,
        [FromBody] UnassignDriverRequest? request = null,
        CancellationToken ct = default)
    {
        try
        {
            var command = new UnassignDriverCommand(
                id,
                driverId,
                request?.Reason,
                request?.AdminNote,
                request?.NotifyDriver ?? false,
                request?.NotifyCustomer ?? false);

            var result = await mediator.Send(command, ct);

            if (result.IsError)
            {
                logger.LogError("Failed to unassign driver: {Error}", result.FirstError.Description);
                return Problem(result.Errors.ToList());
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unassigning driver");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to unassign driver");
        }
    }

    [HttpPost("{id}/notes")]
    public async Task<IActionResult> AddAdminNote(
        [FromRoute] Guid id,
        [FromBody] AddAdminNoteRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var command = new AddAdminNoteCommand(
                id,
                request.Note,
                request.IsInternal,
                request.Category);

            var result = await mediator.Send(command, ct);

            if (result.IsError)
            {
                logger.LogError("Failed to add admin note: {Error}", result.FirstError.Description);
                return Problem(result.Errors.ToList());
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding admin note");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add admin note");
        }
    }

    [HttpPost("cleanup-expired")]
    public async Task<IActionResult> CleanupExpired(CancellationToken ct)
    {
        // Clean up expired sessions (which will cascade to quotes)
        var db = HttpContext.RequestServices.GetRequiredService<Infrastructure.TranzrMovesDbContext>();
        var now = timeService.Now();

        var expiredSessions = await db.Set<QuoteSession>()
            .Where(q => q.ExpiresUtc != null && q.ExpiresUtc < now)
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("Cleaned up {SessionCount} expired sessions", expiredSessions);

        return Ok(new { expiredSessions });
    }

    // --- Quote V2 (resumable journey + optimistic concurrency) ---

    [MapToApiVersion(2)]
    [HttpPost("init")]
    [SwaggerOperation(
        OperationId = "QuoteV2_InitJourney",
        Summary = "Initialize or load Quote V2 for the guest session",
        Description =
            "Creates or returns the in-progress Quote V2 for the authenticated guest cookie (`tranzr_guest`) and selected `quoteType`. " +
            "Call `POST .../ensure` first so the cookie exists. " +
            "Response body is `QuoteJourneyResponse` (`journey` + `quote`). " +
            "Use `ETag` or `quote.version` as `If-Match` on every mutating PATCH.",
        Tags = new[] { "Quote (v2)" })]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InitQuoteV2(
        [FromBody] InitQuoteV2Request request,
        CancellationToken ct)
    {
        var guestId = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Unauthorized();
        }

        var response = await mediator.Send(new InitQuoteJourneyCommand(guestId, request.QuoteType), ct);
        return response.Match(OkWithQuoteJourneyEtag, Problem);
    }

    [MapToApiVersion(2)]
    [HttpPatch("{quoteId:guid}/collection-delivery-addresses")]
    [SwaggerOperation(
        OperationId = "QuoteV2_PatchAddresses",
        Summary = "Update collection and delivery addresses",
        Description =
            "Request JSON is `PatchAddressesRequest` (`addresses`: list of `QuoteAddressDto`). " +
            "Persists origin/destination (and related) addresses, recalculates route distances where applicable, and returns updated journey + quote. " +
            "**If-Match** (required): numeric row version from `quote.version` / last `ETag`. " +
            "Returns **412** when the quote changed concurrently (`Quote.ConcurrencyConflict`). " +
            "Returns **400** when `If-Match` is missing or not a parseable version.",
        Tags = new[] { "Quote (v2)" })]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAddresses(
        Guid quoteId,
        [FromBody] PatchAddressesRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!TryParseQuoteVersion(ifMatch, out var expectedVersion))
        {
            return IfMatchRequiredBadRequest();
        }

        var response = await mediator.Send(new PatchAddressesCommand
        {
            QuoteId = quoteId,
            ExpectedVersion = expectedVersion,
            Addresses = request.Addresses
        }, ct);

        return response.Match(OkWithQuoteJourneyEtag, Problem);
    }

    [MapToApiVersion(2)]
    [HttpPatch("{quoteId:guid}/inventory")]
    [SwaggerOperation(
        OperationId = "QuoteV2_PatchInventory",
        Summary = "Replace quote inventory items",
        Description =
            "Request JSON is `PatchInventoryRequest` (`inventoryItems`). " +
            "Replaces the quote's inventory list with the payload. Inventory item `id` values must be valid **GUIDs** (or omit for new rows per server rules). " +
            "**If-Match** (required): `quote.version` from the last successful read/write. " +
            "**412** on optimistic concurrency conflict.",
        Tags = new[] { "Quote (v2)" })]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchInventory(
        Guid quoteId,
        [FromBody] PatchInventoryRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!TryParseQuoteVersion(ifMatch, out var expectedVersion))
        {
            return IfMatchRequiredBadRequest();
        }

        var command = new PatchInventoryCommand
        {
            QuoteId = quoteId,
            ExpectedVersion = expectedVersion,
            InventoryItems = request.InventoryItems
        };
        var response = await mediator.Send(command, ct);
        return response.Match(OkWithQuoteJourneyEtag, Problem);
    }

    [MapToApiVersion(2)]
    [HttpPatch("{quoteId:guid}/move-date-time")]
    [SwaggerOperation(
        OperationId = "QuoteV2_PatchMoveDateTime",
        Summary = "Update move schedule (collection / delivery / slot)",
        Description =
            "Request JSON is `PatchMoveDateTimeStepRequest`: `schedule` (`ScheduleV2Dto`) plus `selectedVanCount`. " +
            "Updates the quote's schedule (collection / delivery instants, flexible flag, time slot, etc.) and persists van count for pricing. " +
            "**If-Match** (required): current `quote.version` (not duplicated in the body). **412** on concurrency conflict.",
        Tags = new[] { "Quote (v2)" })]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchMoveDateTime(
        Guid quoteId,
        [FromBody] PatchMoveDateTimeStepRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!TryParseQuoteVersion(ifMatch, out var expectedVersion))
        {
            return IfMatchRequiredBadRequest();
        }

        var mapper = new QuoteMapper();
        var schedule = mapper.ToSchedule(request.Schedule);
        if (schedule is null)
        {
            return BadRequest("Schedule is required.");
        }

        schedule.QuoteId = quoteId;

        var command = new PatchMoveDateTimeStepCommand
        {
            QuoteId = quoteId,
            ExpectedVersion = expectedVersion,
            Schedule = schedule,
            SelectedVanCount = request.SelectedVanCount
        };

        var response = await mediator.Send(command, ct);
        return response.Match(OkWithQuoteJourneyEtag, Problem);
    }

    [MapToApiVersion(2)]
    [HttpPatch("{quoteId:guid}/customer-info")]
    [SwaggerOperation(
        OperationId = "QuoteV2_PatchCustomerInfo",
        Summary = "Update customer name and billing address",
        Description =
            "Request JSON is `PatchCustomerInfoStepRequest` (name, billing same-as-origin flag, optional `address`). " +
            "Updates customer profile fields on the quote. " +
            "**If-Match** (required): current `quote.version`. **412** on concurrency conflict.",
        Tags = new[] { "Quote (v2)" })]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchCustomerInfo(
        Guid quoteId,
        [FromBody] PatchCustomerInfoStepRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!TryParseQuoteVersion(ifMatch, out var expectedVersion))
        {
            return IfMatchRequiredBadRequest();
        }

        var command = new PatchCustomerInfoStepCommand
        {
            QuoteId = quoteId,
            ExpectedVersion = expectedVersion,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsBillingAddressSameAsOrigin = request.IsBillingAddressSameAsOrigin,
            Address = request.Address
        };
        var response = await mediator.Send(command, ct);
        return response.Match(OkWithQuoteJourneyEtag, Problem);
    }

    [MapToApiVersion(2)]
    [HttpPatch("{quoteId:guid}/pricing")]
    [SwaggerOperation(
        OperationId = "QuoteV2_PatchPricing",
        Summary = "Select pricing option and van / dismantle options",
        Description =
            "Request JSON is `PatchPricingStepRequest` (`pricingId`, van and dismantle/assembly counts). " +
            "Runs pricing selection for the quote (tier, vans, dismantle/assemble counts). Preconditions (inventory, distance) are enforced server-side. " +
            "**If-Match** (required): current `quote.version`. **412** on concurrency conflict.",
        Tags = new[] { "Quote (v2)" })]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchPricing(
        Guid quoteId,
        [FromBody] PatchPricingStepRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!TryParseQuoteVersion(ifMatch, out var expectedVersion))
        {
            return IfMatchRequiredBadRequest();
        }

        var command = new PatchPricingStepCommand
        {
            QuoteId = quoteId,
            ExpectedVersion = expectedVersion,
            PricingId = request.PricingId,
            NumberOfSelectedVans = request.NumberOfSelectedVans,
            NumberOfItemsToDismantle = request.NumberOfItemsToDismantle,
            NumberOfItemsToAssemble = request.NumberOfItemsToAssemble
        };
        var response = await mediator.Send(command, ct);
        return response.Match(OkWithQuoteJourneyEtag, Problem);
    }

    [MapToApiVersion(2)]
    [HttpPatch("{quoteId:guid}/quote-summary")]
    [SwaggerOperation(
        OperationId = "QuoteV2_PatchQuoteSummary",
        Summary = "Confirm quote summary step",
        Description =
            "Request JSON is `PatchQuoteSummaryStepRequest` (typically empty object `{}`). " +
            "Marks the summary step when server-side completion rules are satisfied. " +
            "**If-Match** (required): current `quote.version`. **412** on concurrency conflict.",
        Tags = new[] { "Quote (v2)" })]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchQuoteSummary(
        Guid quoteId,
        [FromBody] PatchQuoteSummaryStepRequest _,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!TryParseQuoteVersion(ifMatch, out var expectedVersion))
        {
            return IfMatchRequiredBadRequest();
        }

        var command = new PatchSummaryStepCommand
        {
            QuoteId = quoteId,
            ExpectedVersion = expectedVersion
        };
        var response = await mediator.Send(command, ct);
        return response.Match(OkWithQuoteJourneyEtag, Problem);
    }

    [MapToApiVersion(2)]
    [HttpPatch("{quoteId:guid}/customer-email-phone")]
    [SwaggerOperation(
        OperationId = "QuoteV2_PatchCustomerEmailPhone",
        Summary = "Update customer email and phone",
        Description =
            "Request JSON is `PatchCustomerEmailPhoneRequest` (`email`, `phoneNumber`). " +
            "Sets or creates the quote customer's email and phone for downstream steps. " +
            "**If-Match** (required): current `quote.version`. **412** on concurrency conflict.",
        Tags = new[] { "Quote (v2)" })]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchCustomerEmailPhoneAsync(
        Guid quoteId,
        [FromBody] PatchCustomerEmailPhoneRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!TryParseQuoteVersion(ifMatch, out var expectedVersion))
        {
            return IfMatchRequiredBadRequest();
        }

        var command = new PatchCustomerEmailAndPhoneCommand
        {
            QuoteId = quoteId,
            ExpectedVersion = expectedVersion,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };
        var response = await mediator.Send(command, ct);
        return response.Match(OkWithQuoteJourneyEtag, Problem);
    }

    [MapToApiVersion(2)]
    [HttpGet("{quoteId:guid}/journey-state")]
    [SwaggerOperation(
        OperationId = "QuoteV2_GetJourneyState",
        Summary = "Get current journey state and quote snapshot",
        Description =
            "Returns `QuoteJourneyResponse` for hydration (reload, deep link, after 412). " +
            "Response includes `ETag` equal to `quote.version`. " +
            "Send **If-None-Match** with the last ETag to receive **304 Not Modified** when unchanged.",
        Tags = new[] { "Quote (v2)" })]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJourneyState(
        Guid quoteId,
        CancellationToken ct)
    {
        var response = await mediator.Send(new QuoteJourneyStateQuery(quoteId), ct);
        return response.Match(
            value =>
            {
                var etag = value.Quote.Version.ToString(CultureInfo.InvariantCulture);
                Response.Headers.ETag = etag;
                if (EntityTagsMatch(Request.Headers.IfNoneMatch.FirstOrDefault(), etag))
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }

                return Ok(value);
            },
            Problem);
    }

    [MapToApiVersion(2)]
    [HttpPost("resume")]
    [SwaggerOperation(
        OperationId = "QuoteV2_ResumeFromToken",
        Summary = "Evaluate resume token and return journey decision",
        Description =
            "Accepts a signed resume `token` (from email/link flows). " +
            "On success when resumable, returns **`QuoteJourneyState`** (not the full `QuoteJourneyResponse`). " +
            "Use `GET .../journey-state` with the returned `quoteId` to hydrate the full quote + journey. " +
            "Returns **400** for invalid/expired token, **401** when session does not match token, **404** if quote missing.",
        Tags = new[] { "Quote (v2)" })]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QuoteJourneyState), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(QuoteJourneyState), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeQuote(
        [FromBody] ResumeQuoteRequest request,
        CancellationToken ct)
    {
        QuoteResumeTokenPayload payload;
        try
        {
            payload = quoteResumeTokenService.Read(request.Token);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException or System.Security.Cryptography.CryptographicException)
        {
            logger.LogWarning(ex, "Invalid or expired quote resume token");
            return BadRequest(new { message = "Invalid or expired resume token." });
        }

        var quote = await quoteRepository.GetQuoteByIdAsync(payload.QuoteId, ct);
        if (quote is null)
            return NotFound();

        if (!string.Equals(quote.SessionId, payload.SessionId, StringComparison.Ordinal))
            return Unauthorized();

        var decision = quoteResumeResolver.Resolve(quote);

        return decision.IsResumable ? Ok(decision) : BadRequest(decision);
    }

    private IActionResult OkWithQuoteJourneyEtag(QuoteJourneyResponse body)
    {
        Response.Headers.ETag = body.Quote.Version.ToString(CultureInfo.InvariantCulture);
        return Ok(body);
    }

    private BadRequestObjectResult IfMatchRequiredBadRequest() =>
        BadRequest(new
        {
            message =
                $"If-Match is required and must be the numeric quote row version (same as {nameof(QuoteSnapshotDto.Version)} from the last journey response).",
            code = "Quote.IfMatch.Required"
        });

    /// <summary>
    /// Parses If-Match (or similar) into the quote row version. Accepts a plain integer, quoted value, or weak ETag form.
    /// </summary>
    private static bool TryParseQuoteVersion(string? ifMatchHeader, out uint version)
    {
        version = 0;
        if (string.IsNullOrWhiteSpace(ifMatchHeader))
        {
            return false;
        }

        var trimmed = ifMatchHeader.Trim();
        if (trimmed.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..].Trim();
        }

        var comma = trimmed.IndexOf(',');
        if (comma >= 0)
        {
            trimmed = trimmed[..comma].Trim();
        }

        trimmed = trimmed.Trim('"');
        if (string.Equals(trimmed, "*", StringComparison.Ordinal))
        {
            return false;
        }

        return uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out version);
    }

    private static bool EntityTagsMatch(string? headerValue, string candidate)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return false;
        }

        var trimmed = headerValue.Trim();
        if (trimmed.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..].Trim();
        }

        trimmed = trimmed.Trim('"');
        return string.Equals(trimmed, candidate, StringComparison.Ordinal);
    }

    private void RefreshCookie(string guestId)
    {
        Response.Cookies.Append(CookieName, guestId, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(60)
        });
    }
}

// Request DTOs
public record UpdateQuoteStatusRequest(
    string Status,
    string? Reason = null,
    string? AdminNote = null,
    bool NotifyCustomer = false,
    bool NotifyDriver = false);

public record AssignDriverRequest(
    string? Reason = null,
    string? AdminNote = null,
    bool NotifyDriver = false,
    bool NotifyCustomer = false);

public record UnassignDriverRequest(
    string? Reason = null,
    string? AdminNote = null,
    bool NotifyDriver = false,
    bool NotifyCustomer = false);

public record AddAdminNoteRequest(
    string Note,
    bool IsInternal = true,
    string? Category = null);

public sealed record InitQuoteV2Request(QuoteType QuoteType);
