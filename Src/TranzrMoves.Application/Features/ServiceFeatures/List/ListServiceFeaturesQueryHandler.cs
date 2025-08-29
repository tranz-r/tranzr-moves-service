using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.ServiceFeatures.List;

public class ListServiceFeaturesQueryHandler(
    IServiceFeatureRepository serviceFeatureRepository,
    ILogger<ListServiceFeaturesQueryHandler> logger) 
    : IRequestHandler<ListServiceFeaturesQuery, ErrorOr<List<ServiceFeatureDto>>>
{
    public async ValueTask<ErrorOr<List<ServiceFeatureDto>>> Handle(
        ListServiceFeaturesQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var serviceFeatures = await serviceFeatureRepository.GetServiceFeaturesAsync(query.IsActive, cancellationToken);
            
            var mapper = new ServiceFeatureMapper();
            var serviceFeatureDtos = mapper.ToDtoList(serviceFeatures);
            
            logger.LogInformation("Successfully retrieved {Count} service features", serviceFeatureDtos.Count);
            return serviceFeatureDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving service features");
            return Error.Failure("ServiceFeature.ListError", "An error occurred while retrieving service features");
        }
    }
}
