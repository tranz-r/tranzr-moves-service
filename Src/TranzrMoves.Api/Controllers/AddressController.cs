using GetAddress;
using Microsoft.AspNetCore.Mvc;

namespace TranzrMoves.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AddressController(GetAddress.Api api, ILogger<AddressController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SuccessfulAutocomplete>> GetAddressesAsync([FromQuery] string postCode)
    {
        logger.LogInformation("Getting addresses for the postcode {postCode}", postCode);
        
        var autocompleteResult = await api.Autocomplete(postCode);

        if (autocompleteResult.IsSuccess)
        {
            return autocompleteResult.Success;
        }

        logger.LogError("Getting addresses for the postcode {postCode} failed", postCode);
        var errorMessage = autocompleteResult.Failed.Message;
        return BadRequest(new { errorMessage = errorMessage });
    }
}