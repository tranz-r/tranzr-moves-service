using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.ServiceFeatures.Delete;

public class DeleteServiceFeatureCommandHandler(
    IServiceFeatureRepository serviceFeatureRepository,
    ILogger<DeleteServiceFeatureCommandHandler> logger) 
    : IRequestHandler<DeleteServiceFeatureCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(
        DeleteServiceFeatureCommand command,
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

            await serviceFeatureRepository.DeleteServiceFeatureAsync(existingServiceFeature, cancellationToken);
            
            logger.LogInformation("Successfully deleted service feature {Id}", command.Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting service feature with ID {Id}", command.Id);
            return Error.Failure("ServiceFeature.DeleteError", "An error occurred while deleting the service feature");
        }
    }
}
