using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TranzrMoves.Application.Services;

namespace TranzrMoves.Api.Controllers;

[ApiVersion(1)]
[ApiVersion(2)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class MapController(IMapBoxService mapBoxService,
    ILogger<MapController> logger) : ApiControllerBase
{
    [MapToApiVersion("1")]
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
        catch (InvalidOperationException ex) when (ex.Message == "No route found.")
        {
            logger.LogWarning("No drivable route found from {OriginAddress} to {DestinationAddress}", originAddress,
                destinationAddress);
            return NotFound(new
            {
                error = "No drivable route found for the supplied addresses. Please verify the destination details."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get route from {OriginAddress} to {DestinationAddress}", originAddress, destinationAddress);
            return BadRequest(new { error = "Failed to calculate route. Please check your addresses." });
        }
    }

    // [MapToApiVersion("2")]
    // [HttpGet("route")]
    // public async Task<IActionResult> GetRouteAsyncV2(
    //     [FromQuery] Guid quoteId,
    //     [FromQuery] string originAddress,
    //     [FromQuery] string destinationAddress,
    //     CancellationToken cancellationToken)
    // {
    //     var response = await mediator.Send(
    //             new PatchAddressesCommand
    //             {
    //                 QuoteId = quoteId,
    //                 DestinationAddress = destinationAddress,
    //                 OriginAddress = originAddress
    //             }, cancellationToken);
    //
    //     return response.Match(Ok, Problem);
    // }
}
