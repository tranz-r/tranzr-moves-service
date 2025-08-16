using GetAddress;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class AddressController(IMediator mediator, ILogger<AddressController> logger, GetAddress.Api api) : ApiControllerBase
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
    public async Task<double> GetDrivingDistanceAsync([FromQuery] string originAddress, [FromQuery] string destinationAddress, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new TranzrMoves.Application.Features.Addresses.GetDrivingDistance.GetDrivingDistanceQuery(originAddress, destinationAddress), cancellationToken);
        return result.miles;
    }
}