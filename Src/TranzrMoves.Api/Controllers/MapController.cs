using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Api.Controllers;

[Route("api/v1/[controller]")]
public class MapController(IMapBoxService mapBoxService, ILogger<MapController> logger) : ApiControllerBase
{
    [HttpGet("route")]
    public async Task<ActionResult<MapRouteDto>> GetRouteAsync(
        [FromQuery] string originAddress, 
        [FromQuery] string destinationAddress, 
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting route from {OriginAddress} to {DestinationAddress}", originAddress, destinationAddress);
            
            var routeData = await mapBoxService.GetRouteDataAsync(originAddress, destinationAddress, cancellationToken);
            
            return Ok(routeData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get route from {OriginAddress} to {DestinationAddress}", originAddress, destinationAddress);
            return BadRequest(new { error = "Failed to calculate route. Please check your addresses." });
        }
    }
}
