namespace TranzrMoves.Domain.Interfaces;

public interface IMapBoxService
{
    Task<(double km, double miles, double seconds)> GetDrivingDistanceAsync(
        string originAddressOrPostcode,
        string destAddressOrPostcode,
        bool useTraffic = true);
}