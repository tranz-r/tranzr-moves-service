using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Common.CustomErrors;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.AdditionalPrices.Get;

public class GetAdditionalPriceQueryHandler(
    IAdditionalPriceRepository additionalPriceRepository,
    ILogger<GetAdditionalPriceQueryHandler> logger) 
    : IRequestHandler<GetAdditionalPriceQuery, ErrorOr<AdditionalPriceDto>>
{
    public async ValueTask<ErrorOr<AdditionalPriceDto>> Handle(
        GetAdditionalPriceQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var additionalPrice = await additionalPriceRepository.GetAdditionalPriceAsync(query.Id, cancellationToken);
            
            if (additionalPrice is null)
            {
                logger.LogWarning("Additional price not found with ID {Id}", query.Id);
                return Error.Custom((int)CustomErrorType.NotFound, "AdditionalPrice.NotFound", "Additional price not found");
            }

            var mapper = new AdditionalPriceMapper();
            var additionalPriceDto = mapper.ToDto(additionalPrice);
            
            logger.LogInformation("Successfully retrieved additional price {Id}", query.Id);
            return additionalPriceDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving additional price with ID {Id}", query.Id);
            return Error.Failure("AdditionalPrice.RetrievalError", "An error occurred while retrieving the additional price");
        }
    }
}
