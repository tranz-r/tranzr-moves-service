using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class ServiceFeatureMapper
{
    public partial ServiceFeatureDto ToDto(ServiceFeature serviceFeature);
    public partial List<ServiceFeatureDto> ToDtoList(List<ServiceFeature> serviceFeatures);
    
    [MapperIgnoreTarget(nameof(ServiceFeature.Id))]
    public partial ServiceFeature ToEntity(ServiceFeatureDto serviceFeatureDto);
}