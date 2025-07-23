using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Api.Dtos;

namespace TranzrMoves.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("role")]
    public async Task<ActionResult<PaymentSheetCreateResponse>> CreateUserRoleAsync([FromBody] PaymentSheetRequest paymentSheetRequest)
    {
        throw new NotImplementedException();
    }
}