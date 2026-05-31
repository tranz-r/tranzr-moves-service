using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TranzrMoves.Api.Dtos;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Checkout.CreateQuoteV2CheckoutSession;
using TranzrMoves.Application.Features.Checkout.DepositBalance;
using TranzrMoves.Application.Features.Checkout.PaymentSheet;
using TranzrMoves.Application.Features.Checkout.ReadIntent;
using TranzrMoves.Application.Features.Checkout.ReadSession;
using TranzrMoves.Application.Features.Checkout.Webhook;

namespace TranzrMoves.Api.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class CheckoutController(IMediator mediator) : ApiControllerBase
{
    [MapToApiVersion(2)]
    [HttpPost("deposit-balance-payment", Name = "CreateFuturePaymentV2")]
    [SwaggerOperation(
        OperationId = "Checkout_CreateDepositBalancePaymentV2",
        Summary = "Charge remaining balance after deposit (QuoteV2)",
        Description =
            "Creates an off-session PaymentIntent for the amount still owed after a paid deposit, " +
            "using the payment method saved on the paid deposit `Payment` row. " +
            "Body includes `quoteReference` and optional extra charges; resolves a QuoteV2 by reference.",
        Tags = new[] { "Checkout (v2)" })]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFuturePaymentV2([FromBody] FuturePaymentRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateQuoteV2DepositBalancePaymentCommand(request), ct);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var v = result.Value;
        return Ok(new PaymentIntentResponse
        {
            ClientSecret = v.ClientSecret,
            PaymentIntentId = v.IntentId
        });
    }

    [MapToApiVersion(2)]
    [HttpGet("payment-intent", Name = "GetPaymentIntentV2")]
    [SwaggerOperation(
        OperationId = "Checkout_GetPaymentIntentV2",
        Summary = "Retrieve Stripe PaymentIntent or SetupIntent client secret",
        Description =
            "Pass a Stripe id (`pi_...` or `seti_...`) to retrieve client secret for the mobile SDK.",
        Tags = new[] { "Checkout (v2)" })]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentIntentV2([FromQuery] string paymentIntentId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetStripePaymentIntentSecretQuery(paymentIntentId), ct);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var v = result.Value;
        return Ok(new PaymentIntentResponse
        {
            ClientSecret = v.ClientSecret,
            PaymentIntentId = v.IntentId
        });
    }

    [MapToApiVersion(2)]
    [HttpGet("payment-intent-by-quote", Name = "GetPaymentIntentByQuoteIdV2")]
    [SwaggerOperation(
        OperationId = "Checkout_GetPaymentIntentByQuoteIdV2",
        Summary = "Retrieve Stripe PaymentIntent or SetupIntent by QuoteV2 id",
        Description =
            "Loads the customer-selected Full/Deposit/Later payment row (or the latest with an intent) " +
            "and returns the Stripe client secret and intent id (`pi_...` or `seti_...`) for the mobile SDK.",
        Tags = new[] { "Checkout (v2)" })]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentIntentByQuoteIdV2([FromQuery] Guid quoteId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetQuoteV2StripeIntentSecretQuery(quoteId), ct);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var v = result.Value;
        return Ok(new PaymentIntentResponse
        {
            ClientSecret = v.ClientSecret,
            PaymentIntentId = v.IntentId
        });
    }

    [MapToApiVersion(2)]
    [HttpPost("payment-sheet", Name = "CreateStripeIntentV2")]
    [SwaggerOperation(
        OperationId = "Checkout_CreatePaymentSheetV2",
        Summary = "Create Stripe payment sheet from persisted QuoteV2",
        Description =
            "Creates a Stripe payment/setup intent using only persisted QuoteV2 data. " +
            "Request includes quoteId, expectedVersion, and paymentType for optimistic concurrency " +
            "and explicit customer payment option selection.",
        Tags = new[] { "Checkout (v2)" })]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(PaymentSheetCreateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> CreatePaymentSheetV2(
        [FromBody] CreateQuoteV2PaymentSheetRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateQuoteV2PaymentSheetCommand(request.QuoteId, request.ExpectedVersion, request.PaymentType),
            ct);

        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var v = result.Value;
        return Ok(new PaymentSheetCreateResponse
        {
            PaymentIntent = v.ClientSecret,
            PaymentIntentId = v.IntentId,
            EphemeralKey = v.EphemeralKey,
            Customer = v.CustomerId
        });
    }

    [MapToApiVersion(2)]
    [HttpGet("session")]
    [SwaggerOperation(
        OperationId = "Checkout_GetCheckoutSessionV2",
        Summary = "Retrieve Stripe Checkout session by id",
        Description = "Loads the Checkout Session from Stripe (session id from hosted checkout).",
        Tags = new[] { "Checkout (v2)" })]
    [ProducesResponseType(typeof(GetCheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCheckoutSessionV2([FromQuery] string id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetStripeCheckoutSessionQuery(id), ct);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var s = result.Value;
        return Ok(new GetCheckoutSessionResponse
        {
            SessionId = s.SessionId,
            CustomerId = s.CustomerId,
            PaymentIntentId = s.PaymentIntentId,
            Status = s.Status,
            PaymentStatus = s.PaymentStatus,
            Url = s.Url
        });
    }

    [MapToApiVersion(2)]
    [HttpPost("session")]
    [SwaggerOperation(
        OperationId = "Checkout_CreateQuoteV2CheckoutSession",
        Summary = "Create hosted Stripe Checkout session (QuoteV2)",
        Description =
            "Creates a Stripe Checkout Session from persisted QuoteV2 data with optimistic concurrency (expectedVersion).",
        Tags = new[] { "Checkout (v2)" })]
    [ProducesResponseType(typeof(CreateQuoteV2CheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> CreateCheckoutSessionV2(
        [FromBody] CreateQuoteV2CheckoutSessionRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateQuoteV2CheckoutSessionCommand(
                request.QuoteId,
                request.ExpectedVersion,
                request.Amount,
                request.Description ?? string.Empty),
            ct);

        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(2)]
    [HttpPost("webhook")]
    [SwaggerOperation(
        OperationId = "Checkout_StripeWebhookV2",
        Summary = "Stripe webhook (QuoteV2 pipeline)",
        Description =
            "Uses TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2. Configure a separate Stripe webhook endpoint for /api/v2/checkout/webhook.",
        Tags = new[] { "Checkout (v2)" })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessPaymentWebhookV2(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();
        var result = await mediator.Send(new ProcessCheckoutStripeWebhookV2Command(json, signatureHeader), ct);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        return Ok();
    }
}
