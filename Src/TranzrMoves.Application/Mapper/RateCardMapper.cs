using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class RateCardMapper
{
    public partial RateCardDto ToDto(RateCard rateCard);
    public partial List<RateCardDto> ToDtoList(List<RateCard> rateCards);

    [MapperIgnoreTarget(nameof(RateCard.Id))]
    public partial RateCard ToEntity(RateCardDto rateCardDto);
}