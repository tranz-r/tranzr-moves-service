using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.AdditionalPrices.Delete;

public class DeleteAdditionalPriceCommandHandler(
    IAdditionalPriceRepository additionalPriceRepository,
    ILogger<DeleteAdditionalPriceCommandHandler> logger) 
    : IRequestHandler<DeleteAdditionalPriceCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(
        DeleteAdditionalPriceCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingAdditionalPrice = await additionalPriceRepository.GetAdditionalPriceAsync(command.Id, cancellationToken);
            
            if (existingAdditionalPrice is null)
            {
                logger.LogWarning("Additional price not found with ID {Id}", command.Id);
                return Error.Custom((int)CustomErrorType.NotFound, "AdditionalPrice.NotFound", "Additional price not found");
            }

            await additionalPriceRepository.DeleteAdditionalPriceAsync(existingAdditionalPrice, cancellationToken);
            
            logger.LogInformation("Successfully deleted additional price {Id}", command.Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting additional price with ID {Id}", command.Id);
            return Error.Failure("AdditionalPrice.DeleteError", "An error occurred while deleting the additional price");
        }
    }
}
