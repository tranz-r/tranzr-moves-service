using Mediator;

namespace TranzrMoves.Application.Features.Addresses.GetDrivingDistance;

public sealed record GetDrivingDistanceQuery(string OriginAddress, string DestinationAddress) : IQuery<(double km, double miles, double seconds)>;
