using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public class MapBoxService(HttpClient client, ILogger<MapBoxService> logger, IConfiguration configuration) : IMapBoxService
{
    public async Task<(double km, double miles, double seconds)> GetDrivingDistanceAsync(
        string originAddressOrPostcode,
        string destAddressOrPostcode,
        bool useTraffic = true)
    {
        var mapboxToken = configuration.GetSection("MAPBOX_TOKEN").Value;
        var (lon1, lat1) = await GeocodeAsync(originAddressOrPostcode, mapboxToken);
        var (lon2, lat2) = await GeocodeAsync(destAddressOrPostcode, mapboxToken);

        var profile = useTraffic ? "mapbox/driving-traffic" : "mapbox/driving";
        var directionsUrl =
            $"directions/v5/{profile}/{lon1},{lat1};{lon2},{lat2}" +
            $"?overview=false&alternatives=false&access_token={mapboxToken}";

        using var res = await client.GetAsync(directionsUrl);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        var routes = doc.RootElement.GetProperty("routes");
        if (routes.GetArrayLength() == 0)
            throw new InvalidOperationException("No route found.");

        var route = routes[0];
        var distanceMeters = route.GetProperty("distance").GetDouble();
        var durationSeconds = route.GetProperty("duration").GetDouble();

        var km = distanceMeters / 1000.0;
        var miles = distanceMeters / 1609.344;

        return (km, miles + 1, durationSeconds);
    }
    
    public async Task<MapRouteDto> GetRouteDataAsync(
        string originAddress,
        string destinationAddress,
        CancellationToken cancellationToken = default)
    {
        var mapboxToken = configuration.GetSection("MAPBOX_TOKEN").Value;
        
        // Geocode both addresses
        var (originLon, originLat, originFormattedAddress) = await GeocodeWithAddressAsync(originAddress, mapboxToken);
        var (destLon, destLat, destFormattedAddress) = await GeocodeWithAddressAsync(destinationAddress, mapboxToken);

        // Get route with full geometry
        var profile = "mapbox/driving";
        var directionsUrl =
            $"directions/v5/{profile}/{originLon},{originLat};{destLon},{destLat}" +
            $"?overview=full&geometries=geojson&access_token={mapboxToken}";

        using var res = await client.GetAsync(directionsUrl, cancellationToken);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var routes = doc.RootElement.GetProperty("routes");
        if (routes.GetArrayLength() == 0)
            throw new InvalidOperationException("No route found.");

        var route = routes[0];
        var distanceMeters = route.GetProperty("distance").GetDouble();
        var durationSeconds = route.GetProperty("duration").GetDouble();
        var geometry = route.GetProperty("geometry");

        // Parse GeoJSON LineString coordinates
        var coordinates = geometry.GetProperty("coordinates");
        var routeCoordinates = new List<CoordinateDto>();
        
        foreach (var coord in coordinates.EnumerateArray())
        {
            routeCoordinates.Add(new CoordinateDto
            {
                Longitude = coord[0].GetDouble(),
                Latitude = coord[1].GetDouble()
            });
        }

        return new MapRouteDto
        {
            Coordinates = routeCoordinates,
            DistanceMiles = distanceMeters / 1609.344,
            DurationMinutes = durationSeconds / 60.0,
            Origin = new OriginDestinationDto
            {
                Longitude = originLon,
                Latitude = originLat,
                Address = originFormattedAddress
            },
            Destination = new OriginDestinationDto
            {
                Longitude = destLon,
                Latitude = destLat,
                Address = destFormattedAddress
            }
        };
    }
    
    private async Task<(double lon, double lat)> GeocodeAsync(string query, string token)
    {
        var q = HttpUtility.UrlEncode(query);
        var url = $"geocoding/v5/mapbox.places/{q}.json" +
                  $"?country=GB&types=address,postcode&limit=1&autocomplete=false&access_token={token}";

        using var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        var features = doc.RootElement.GetProperty("features");
        if (features.GetArrayLength() == 0)
            throw new InvalidOperationException($"Could not geocode: '{query}'.");

        var coords = features[0].GetProperty("geometry").GetProperty("coordinates");
        var lon = coords[0].GetDouble();
        var lat = coords[1].GetDouble();
        return (lon, lat);
    }
    
    private async Task<(double lon, double lat, string formattedAddress)> GeocodeWithAddressAsync(string query, string token)
    {
        var q = HttpUtility.UrlEncode(query);
        var url = $"geocoding/v5/mapbox.places/{q}.json" +
                  $"?country=GB&types=address,postcode&limit=1&autocomplete=false&access_token={token}";

        using var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        var features = doc.RootElement.GetProperty("features");
        if (features.GetArrayLength() == 0)
            throw new InvalidOperationException($"Could not geocode: '{query}'.");

        var feature = features[0];
        var coords = feature.GetProperty("geometry").GetProperty("coordinates");
        var lon = coords[0].GetDouble();
        var lat = coords[1].GetDouble();
        var formattedAddress = feature.GetProperty("place_name").GetString() ?? query;
        
        return (lon, lat, formattedAddress);
    }
}