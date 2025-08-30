using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.AdditionalPrices.Create;

public class CreateAdditionalPriceCommandHandler(
    IAdditionalPriceRepository additionalPriceRepository,
    ILogger<CreateAdditionalPriceCommandHandler> logger) 
    : IRequestHandler<CreateAdditionalPriceCommand, ErrorOr<AdditionalPriceDto>>
{
    public async ValueTask<ErrorOr<AdditionalPriceDto>> Handle(
        CreateAdditionalPriceCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var additionalPrice = new AdditionalPrice
            {
                Type = command.Type,
                Description = command.Description,
                Price = command.Price,
                CurrencyCode = command.CurrencyCode,
                EffectiveFrom = command.EffectiveFrom,
                EffectiveTo = command.EffectiveTo,
                IsActive = command.IsActive
            };

            var result = await additionalPriceRepository.AddAdditionalPriceAsync(additionalPrice, cancellationToken);
            
            if (result.IsError)
            {
                logger.LogError("Failed to create additional price: {Error}", result.FirstError.Description);
                return result.Errors;
            }

            var mapper = new AdditionalPriceMapper();
            var additionalPriceDto = mapper.ToDto(result.Value);
            
            logger.LogInformation("Successfully created additional price {Id} of type {Type}", 
                additionalPriceDto.Id, additionalPriceDto.Type);

            return additionalPriceDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating additional price");
            return Error.Failure("AdditionalPrice.CreationError", "An error occurred while creating the additional price");
        }
    }
}
