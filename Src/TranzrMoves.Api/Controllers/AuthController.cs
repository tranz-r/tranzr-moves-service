using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Features.Auth.AcceptInvitation;
using TranzrMoves.Application.Features.Auth.Context;
using TranzrMoves.Application.Features.Auth.Register;
using TranzrMoves.Infrastructure.Authentication;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class AuthController(IMediator mediator, ILogger<AuthController> logger) : ApiControllerBase
{
    [HttpPost("register")]
    [SwaggerOperation(
        OperationId = "Auth_RegisterUser",
        Summary = "Create a Supabase auth account and linked app user",
        Description = "Frontend collects registration data and sends it here. The backend provisions the Supabase auth user (OTP sign-in only) and persists the linked app user record.",
        Tags = ["Auth (v1)"])]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering user account for {Email}", command.Email);

        var result = await mediator.Send(command, cancellationToken);
        return result.Match(
            response => Created($"/api/v1/auth/users/{response.UserId}", response),
            Problem);
    }

    [HttpGet("context")]
    [Authorize(Policy = AuthorizationPolicies.BusinessUser)]
    [SwaggerOperation(
        OperationId = "Auth_GetContext",
        Summary = "Get the authenticated business user's context",
        Description = "Resolves UserId, BusinessUserId, BusinessAccountId, Email, and Role from the Supabase JWT. Requires an Active business user.",
        Tags = ["Auth (v1)"])]
    [ProducesResponseType(typeof(AuthContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetContext(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAuthContextQuery(), cancellationToken);
        return result.Match(Ok, Problem);
    }

    [HttpPost("accept-invitation")]
    [Authorize]
    [SwaggerOperation(
        OperationId = "Auth_AcceptInvitation",
        Summary = "Accept a business invitation",
        Description = "Links the authenticated Supabase identity to the invited app user and activates the membership (Invited -> Active). Returns the resolved auth context.",
        Tags = ["Auth (v1)"])]
    [ProducesResponseType(typeof(AuthContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AcceptInvitationCommand(), cancellationToken);
        return result.Match(Ok, Problem);
    }
}
