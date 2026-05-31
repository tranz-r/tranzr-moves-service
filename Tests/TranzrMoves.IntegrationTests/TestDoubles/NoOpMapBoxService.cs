using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.IntegrationTests.TestDoubles;

/// <summary>
/// Stub for worker E2E hosts that load the full application layer but not API-only Mapbox HTTP clients.
/// </summary>
internal sealed class NoOpMapBoxService : IMapBoxService
{
    public Task<(double km, double miles, double seconds)> GetDrivingDistanceAsync(
        string originAddressOrPostcode,
        string destAddressOrPostcode,
        bool useTraffic = true) =>
        Task.FromResult((1.0, 1.0, 60.0));

    public Task<MapRouteDto> GetRouteDataAsync(
        string originAddress,
        string destinationAddress,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MapRouteDto());

    public Task<MapRouteV2> GetRouteDataV2Async(
        string originAddress,
        string destinationAddress,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MapRouteV2());
}
