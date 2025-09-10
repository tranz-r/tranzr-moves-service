using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.RateCards.Get;

public class GetRateCardQueryHandler(
    IRateCardRepository rateCardRepository,
    ILogger<GetRateCardQueryHandler> logger) 
    : IRequestHandler<GetRateCardQuery, ErrorOr<RateCardDto>>
{
    public async ValueTask<ErrorOr<RateCardDto>> Handle(
        GetRateCardQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var rateCard = await rateCardRepository.GetRateCardAsync(query.Id, cancellationToken);
            
            if (rateCard is null)
            {
                logger.LogWarning("Rate card not found with ID {Id}", query.Id);
                return Error.Custom((int)CustomErrorType.NotFound, "RateCard.NotFound", "Rate card not found");
            }

            var mapper = new RateCardMapper();
            var rateCardDto = mapper.ToDto(rateCard);
            
            logger.LogInformation("Successfully retrieved rate card {Id}", query.Id);
            return rateCardDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving rate card with ID {Id}", query.Id);
            return Error.Failure("RateCard.RetrievalError", "An error occurred while retrieving the rate card");
        }
    }
}
