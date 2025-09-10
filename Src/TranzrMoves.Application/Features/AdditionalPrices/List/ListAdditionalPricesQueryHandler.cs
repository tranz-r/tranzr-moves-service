using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Application.Mapper;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.AdditionalPrices.List;

public class ListAdditionalPricesQueryHandler(
    IAdditionalPriceRepository additionalPriceRepository,
    ILogger<ListAdditionalPricesQueryHandler> logger) 
    : IRequestHandler<ListAdditionalPricesQuery, ErrorOr<List<AdditionalPriceDto>>>
{
    public async ValueTask<ErrorOr<List<AdditionalPriceDto>>> Handle(
        ListAdditionalPricesQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var additionalPrices = await additionalPriceRepository.GetAdditionalPricesAsync(query.IsActive, cancellationToken);
            
            var mapper = new AdditionalPriceMapper();
            var additionalPriceDtos = additionalPrices.Select(mapper.ToDto).ToList();
            
            logger.LogInformation("Successfully retrieved {Count} additional prices", additionalPriceDtos.Count);
            return additionalPriceDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving additional prices");
            return Error.Failure("AdditionalPrice.ListError", "An error occurred while retrieving additional prices");
        }
    }
}
