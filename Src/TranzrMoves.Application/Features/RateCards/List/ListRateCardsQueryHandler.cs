using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.RateCards.List;

public class ListRateCardsQueryHandler(
    IRateCardRepository rateCardRepository,
    ILogger<ListRateCardsQueryHandler> logger) 
    : IRequestHandler<ListRateCardsQuery, ErrorOr<List<RateCardDto>>>
{
    public async ValueTask<ErrorOr<List<RateCardDto>>> Handle(
        ListRateCardsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var rateCards = await rateCardRepository.GetRateCardsAsync(query.IsActive, cancellationToken);
            
            var mapper = new RateCardMapper();
            var rateCardDtos = mapper.ToDtoList(rateCards);
            
            logger.LogInformation("Successfully retrieved {Count} rate cards", rateCardDtos.Count);
            return rateCardDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving rate cards");
            return Error.Failure("RateCard.ListError", "An error occurred while retrieving rate cards");
        }
    }
}
