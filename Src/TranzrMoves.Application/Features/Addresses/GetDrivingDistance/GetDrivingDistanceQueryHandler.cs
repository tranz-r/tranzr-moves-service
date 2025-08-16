using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Addresses.GetDrivingDistance;

public sealed class GetDrivingDistanceQueryHandler(IMapBoxService mapBoxService, ILogger<GetDrivingDistanceQueryHandler> logger)
    : IQueryHandler<GetDrivingDistanceQuery, (double km, double miles, double seconds)>
{
    public async ValueTask<(double km, double miles, double seconds)> Handle(GetDrivingDistanceQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting driving distance between {Origin} and {Destination}", query.OriginAddress, query.DestinationAddress);
        return await mapBoxService.GetDrivingDistanceAsync(query.OriginAddress, query.DestinationAddress);
    }
}
