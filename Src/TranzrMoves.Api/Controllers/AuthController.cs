using Mediator;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TranzrMoves.Application.Features.Auth.Register;

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
}
