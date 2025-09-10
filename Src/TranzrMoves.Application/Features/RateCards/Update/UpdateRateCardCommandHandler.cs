using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.RateCards.Update;

public class UpdateRateCardCommandHandler(
    IRateCardRepository rateCardRepository,
    ILogger<UpdateRateCardCommandHandler> logger) 
    : IRequestHandler<UpdateRateCardCommand, ErrorOr<RateCardDto>>
{
    public async ValueTask<ErrorOr<RateCardDto>> Handle(
        UpdateRateCardCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingRateCard = await rateCardRepository.GetRateCardAsync(command.Id, cancellationToken);
            
            if (existingRateCard is null)
            {
                logger.LogWarning("Rate card not found with ID {Id}", command.Id);
                return Error.Custom((int)CustomErrorType.NotFound, "RateCard.NotFound", "Rate card not found");
            }

            // Update the existing rate card
            existingRateCard.Movers = command.Movers;
            existingRateCard.ServiceLevel = command.ServiceLevel;
            existingRateCard.BaseBlockHours = command.BaseBlockHours;
            existingRateCard.BaseBlockPrice = command.BaseBlockPrice;
            existingRateCard.HourlyRateAfter = command.HourlyRateAfter;
            existingRateCard.CurrencyCode = command.CurrencyCode;
            existingRateCard.EffectiveFrom = command.EffectiveFrom;
            existingRateCard.EffectiveTo = command.EffectiveTo;
            existingRateCard.IsActive = command.IsActive;

            var result = await rateCardRepository.UpdateRateCardAsync(existingRateCard, cancellationToken);
            
            if (result.IsError)
            {
                logger.LogError("Failed to update rate card {Id}: {Error}", command.Id, result.FirstError.Description);
                return result.Errors;
            }

            var mapper = new RateCardMapper();
            var rateCardDto = mapper.ToDto(result.Value);
            
            logger.LogInformation("Successfully updated rate card {Id}", command.Id);
            return rateCardDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating rate card with ID {Id}", command.Id);
            return Error.Failure("RateCard.UpdateError", "An error occurred while updating the rate card");
        }
    }
}
