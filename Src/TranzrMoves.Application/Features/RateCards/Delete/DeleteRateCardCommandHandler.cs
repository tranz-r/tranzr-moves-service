using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.RateCards.Delete;

public class DeleteRateCardCommandHandler(
    IRateCardRepository rateCardRepository,
    ILogger<DeleteRateCardCommandHandler> logger) 
    : IRequestHandler<DeleteRateCardCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(
        DeleteRateCardCommand command,
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

            await rateCardRepository.DeleteRateCardAsync(existingRateCard, cancellationToken);
            
            logger.LogInformation("Successfully deleted rate card {Id}", command.Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting rate card with ID {Id}", command.Id);
            return Error.Failure("RateCard.DeleteError", "An error occurred while deleting the rate card");
        }
    }
}
