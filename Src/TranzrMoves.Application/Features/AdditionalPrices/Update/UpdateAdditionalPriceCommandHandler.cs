using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.AdditionalPrices.Update;

public class UpdateAdditionalPriceCommandHandler(
    IAdditionalPriceRepository additionalPriceRepository,
    ILogger<UpdateAdditionalPriceCommandHandler> logger) 
    : IRequestHandler<UpdateAdditionalPriceCommand, ErrorOr<AdditionalPriceDto>>
{
    public async ValueTask<ErrorOr<AdditionalPriceDto>> Handle(
        UpdateAdditionalPriceCommand command,
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

            // Update the existing additional price
            existingAdditionalPrice.Type = command.Type;
            existingAdditionalPrice.Description = command.Description;
            existingAdditionalPrice.Price = command.Price;
            existingAdditionalPrice.CurrencyCode = command.CurrencyCode;
            existingAdditionalPrice.EffectiveFrom = command.EffectiveFrom;
            existingAdditionalPrice.EffectiveTo = command.EffectiveTo;
            existingAdditionalPrice.IsActive = command.IsActive;

            var result = await additionalPriceRepository.UpdateAdditionalPriceAsync(existingAdditionalPrice, cancellationToken);
            
            if (result.IsError)
            {
                logger.LogError("Failed to update additional price {Id}: {Error}", command.Id, result.FirstError.Description);
                return result.Errors;
            }

            var mapper = new AdditionalPriceMapper();
            var additionalPriceDto = mapper.ToDto(result.Value);
            
            logger.LogInformation("Successfully updated additional price {Id}", command.Id);
            return additionalPriceDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating additional price with ID {Id}", command.Id);
            return Error.Failure("AdditionalPrice.UpdateError", "An error occurred while updating the additional price");
        }
    }
}
