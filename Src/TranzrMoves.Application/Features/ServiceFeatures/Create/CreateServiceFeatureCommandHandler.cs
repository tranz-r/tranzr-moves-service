using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.ServiceFeatures.Create;

public class CreateServiceFeatureCommandHandler(
    IServiceFeatureRepository serviceFeatureRepository,
    ILogger<CreateServiceFeatureCommandHandler> logger) 
    : IRequestHandler<CreateServiceFeatureCommand, ErrorOr<ServiceFeatureDto>>
{
    public async ValueTask<ErrorOr<ServiceFeatureDto>> Handle(
        CreateServiceFeatureCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var serviceFeature = new ServiceFeature
            {
                ServiceLevel = command.ServiceLevel,
                Text = command.Text,
                DisplayOrder = command.DisplayOrder,
                EffectiveFrom = command.EffectiveFrom,
                EffectiveTo = command.EffectiveTo,
                IsActive = command.IsActive
            };

            var result = await serviceFeatureRepository.AddServiceFeatureAsync(serviceFeature, cancellationToken);
            
            if (result.IsError)
            {
                logger.LogError("Failed to create service feature: {Error}", result.FirstError.Description);
                return result.Errors;
            }

            var mapper = new ServiceFeatureMapper();
            var serviceFeatureDto = mapper.ToDto(result.Value);
            
            logger.LogInformation("Successfully created service feature {Id} for {ServiceLevel} service level", 
                serviceFeatureDto.Id, serviceFeatureDto.ServiceLevel);

            return serviceFeatureDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating service feature");
            return Error.Failure("ServiceFeature.CreationError", "An error occurred while creating the service feature");
        }
    }
}
