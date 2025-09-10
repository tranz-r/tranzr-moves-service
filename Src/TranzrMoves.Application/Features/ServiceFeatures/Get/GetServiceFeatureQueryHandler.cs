using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.ServiceFeatures.Get;

public class GetServiceFeatureQueryHandler(
    IServiceFeatureRepository serviceFeatureRepository,
    ILogger<GetServiceFeatureQueryHandler> logger) 
    : IRequestHandler<GetServiceFeatureQuery, ErrorOr<ServiceFeatureDto>>
{
    public async ValueTask<ErrorOr<ServiceFeatureDto>> Handle(
        GetServiceFeatureQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var serviceFeature = await serviceFeatureRepository.GetServiceFeatureAsync(query.Id, cancellationToken);
            
            if (serviceFeature is null)
            {
                logger.LogWarning("Service feature not found with ID {Id}", query.Id);
                return Error.Custom((int)CustomErrorType.NotFound, "ServiceFeature.NotFound", "Service feature not found");
            }

            var mapper = new ServiceFeatureMapper();
            var serviceFeatureDto = mapper.ToDto(serviceFeature);
            
            logger.LogInformation("Successfully retrieved service feature {Id}", query.Id);
            return serviceFeatureDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving service feature with ID {Id}", query.Id);
            return Error.Failure("ServiceFeature.RetrievalError", "An error occurred while retrieving the service feature");
        }
    }
}
