namespace TranzrMoves.Domain.Interfaces;

public interface IMapBoxService
{
    Task<(double km, double miles, double seconds)> GetDrivingDistanceAsync(
        string originAddressOrPostcode,
        string destAddressOrPostcode,
        bool useTraffic = true);
    
    Task<MapRouteDto> GetRouteDataAsync(
        string originAddress,
        string destinationAddress,
        CancellationToken cancellationToken = default);
}

public class MapRouteDto
{
    public List<CoordinateDto> Coordinates { get; set; } = new();
    public double DistanceMiles { get; set; }
    public double DurationMinutes { get; set; }
    public OriginDestinationDto Origin { get; set; } = new();
    public OriginDestinationDto Destination { get; set; } = new();
}

public class CoordinateDto
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
}

public class OriginDestinationDto
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public string Address { get; set; } = string.Empty;
}