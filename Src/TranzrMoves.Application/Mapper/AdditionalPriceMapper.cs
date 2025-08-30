using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class AdditionalPriceMapper
{
    public partial AdditionalPriceDto ToDto(AdditionalPrice additionalPrice);

    [MapperIgnoreTarget(nameof(AdditionalPrice.Id))]
    public partial AdditionalPrice ToEntity(AdditionalPriceDto additionalPriceDto);
}