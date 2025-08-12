using GetAddress;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class AddressController(GetAddress.Api api, ILogger<AddressController> logger, IMapBoxService mapBoxService) : ApiControllerBase
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
    
    [HttpGet("distance")]
    public async Task<double> GetDrivingDistanceAsync([FromQuery] string originAddress, [FromQuery] string destinationAddress)
    {
        var result = await mapBoxService.GetDrivingDistanceAsync(originAddress, destinationAddress);

        return result.miles;
    }
}