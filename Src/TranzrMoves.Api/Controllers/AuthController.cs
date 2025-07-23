using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Api.Dtos;

namespace TranzrMoves.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("role", Name = "CreateStripeIntent")]
    public async Task<ActionResult<PaymentSheetCreateResponse>> CreatePaymentSheet([FromBody] PaymentSheetRequest paymentSheetRequest)
    {
        throw new NotImplementedException();
    }
}