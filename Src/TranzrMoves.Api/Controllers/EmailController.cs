using GetAddress;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Features.Contact.SendContactForm;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class EmailController(IMediator mediator, ILogger<EmailController> logger) : ApiControllerBase
{
    [HttpPost("contact-form")]
    public async Task<IActionResult> SendContactForm(
        [FromBody] SendContactFormCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received contact form submission from {Email}", command.Email);

        var result = await mediator.Send(command, cancellationToken);
        return result.Match(Ok, Problem);
    }
}
