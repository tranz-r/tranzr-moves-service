using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.RateCards.Create;

public class CreateRateCardCommandHandler(
    IRateCardRepository rateCardRepository,
    ILogger<CreateRateCardCommandHandler> logger) 
    : IRequestHandler<CreateRateCardCommand, ErrorOr<RateCardDto>>
{
    public async ValueTask<ErrorOr<RateCardDto>> Handle(
        CreateRateCardCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var rateCard = new RateCard
            {
                Movers = command.Movers,
                ServiceLevel = command.ServiceLevel,
                BaseBlockHours = command.BaseBlockHours,
                BaseBlockPrice = command.BaseBlockPrice,
                HourlyRateAfter = command.HourlyRateAfter,
                CurrencyCode = command.CurrencyCode,
                EffectiveFrom = command.EffectiveFrom,
                EffectiveTo = command.EffectiveTo,
                IsActive = command.IsActive
            };

            var result = await rateCardRepository.AddRateCardAsync(rateCard, cancellationToken);
            
            if (result.IsError)
            {
                logger.LogError("Failed to create rate card: {Error}", result.FirstError.Description);
                return result.Errors;
            }

            var mapper = new RateCardMapper();
            var rateCardDto = mapper.ToDto(result.Value);
            
            logger.LogInformation("Successfully created rate card {Id} for {Movers} movers with {ServiceLevel} service level", 
                rateCardDto.Id, rateCardDto.Movers, rateCardDto.ServiceLevel);

            return rateCardDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating rate card");
            return Error.Failure("RateCard.CreationError", "An error occurred while creating the rate card");
        }
    }
}
