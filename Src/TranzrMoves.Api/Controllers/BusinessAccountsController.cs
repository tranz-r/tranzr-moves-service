using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.BusinessAccount.Get;
using TranzrMoves.Application.Features.BusinessAccount.Register;
using TranzrMoves.Application.Features.BusinessAccount.Update;
using TranzrMoves.Infrastructure.Authentication;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class BusinessAccountsController(IMediator mediator, ILogger<BusinessAccountsController> logger)
    : ApiControllerBase
{
    [HttpPost("register")]
    [Authorize]
    [SwaggerOperation(
        OperationId = "BusinessAccount_Register",
        Summary = "Register a business account with the authenticated owner",
        Description = "Requires a Supabase JWT obtained after OTP verification and a Cloudflare Turnstile token. Atomically creates UserV2, BusinessAccount, and BusinessUser (Owner).",
        Tags = ["Business Accounts (v1)"])]
    [ProducesResponseType(typeof(RegisterBusinessAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterBusinessAccountCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering business account for owner {OwnerEmail}", command.Owner.Email);

        var result = await mediator.Send(command, cancellationToken);
        return result.Match(
            response => CreatedAtAction(nameof(Get), new { id = response.BusinessAccountId }, response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.BusinessUser)]
    [SwaggerOperation(
        OperationId = "BusinessAccount_Get",
        Summary = "Get a business account by ID",
        Description = "Returns the business account when the authenticated user is an active member of that tenant.",
        Tags = ["Business Accounts (v1)"])]
    [ProducesResponseType(typeof(BusinessAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBusinessAccountQuery(id), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    [SwaggerOperation(
        OperationId = "BusinessAccount_Update",
        Summary = "Update a business account",
        Description = "Only business account owners may update account details for their tenant.",
        Tags = ["Business Accounts (v1)"])]
    [ProducesResponseType(typeof(BusinessAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBusinessAccountCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("ID in URL does not match ID in request body");
        }

        var result = await mediator.Send(command, cancellationToken);
        return result.Match(Ok, Problem);
    }
}
