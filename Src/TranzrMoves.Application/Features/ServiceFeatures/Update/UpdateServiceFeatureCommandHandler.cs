using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.ServiceFeatures.Update;

public class UpdateServiceFeatureCommandHandler(
    IServiceFeatureRepository serviceFeatureRepository,
    ILogger<UpdateServiceFeatureCommandHandler> logger) 
    : IRequestHandler<UpdateServiceFeatureCommand, ErrorOr<ServiceFeatureDto>>
{
    public async ValueTask<ErrorOr<ServiceFeatureDto>> Handle(
        UpdateServiceFeatureCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingServiceFeature = await serviceFeatureRepository.GetServiceFeatureAsync(command.Id, cancellationToken);
            
            if (existingServiceFeature is null)
            {
                logger.LogWarning("Service feature not found with ID {Id}", command.Id);
                return Error.Custom((int)CustomErrorType.NotFound, "ServiceFeature.NotFound", "Service feature not found");
            }

            // Update the existing service feature
            existingServiceFeature.ServiceLevel = command.ServiceLevel;
            existingServiceFeature.Text = command.Text;
            existingServiceFeature.DisplayOrder = command.DisplayOrder;
            existingServiceFeature.EffectiveFrom = command.EffectiveFrom;
            existingServiceFeature.EffectiveTo = command.EffectiveTo;
            existingServiceFeature.IsActive = command.IsActive;

            var result = await serviceFeatureRepository.UpdateServiceFeatureAsync(existingServiceFeature, cancellationToken);
            
            if (result.IsError)
            {
                logger.LogError("Failed to update service feature {Id}: {Error}", command.Id, result.FirstError.Description);
                return result.Errors;
            }

            var mapper = new ServiceFeatureMapper();
            var serviceFeatureDto = mapper.ToDto(result.Value);
            
            logger.LogInformation("Successfully updated service feature {Id}", command.Id);
            return serviceFeatureDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating service feature with ID {Id}", command.Id);
            return Error.Failure("ServiceFeature.UpdateError", "An error occurred while updating the service feature");
        }
    }
}
