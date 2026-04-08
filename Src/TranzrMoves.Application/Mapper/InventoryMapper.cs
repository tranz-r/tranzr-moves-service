using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class InventoryMapper
{
    public partial InventoryCategoryImportDto ToDto(InventoryCategory src);
    public partial List<InventoryCategoryImportDto> ToDtoList(List<InventoryCategory> src);
    public partial InventoryCategory ToEntity(InventoryCategoryImportDto src);
    public partial List<InventoryCategory> ToEntityList(List<InventoryCategoryImportDto> src);

    [MapProperty(nameof(InventoryGood.LengthCm), nameof(InventoryGoodImportDto.Length))]
    [MapProperty(nameof(InventoryGood.WidthCm), nameof(InventoryGoodImportDto.Width))]
    [MapProperty(nameof(InventoryGood.HeightCm), nameof(InventoryGoodImportDto.Height))]
    public partial InventoryGoodImportDto ToDto(InventoryGood src);

    [MapProperty(nameof(InventoryGoodImportDto.Length), nameof(InventoryGood.LengthCm))]
    [MapProperty(nameof(InventoryGoodImportDto.Width), nameof(InventoryGood.WidthCm))]
    [MapProperty(nameof(InventoryGoodImportDto.Height), nameof(InventoryGood.HeightCm))]
    public partial InventoryGood ToEntity(InventoryGoodImportDto src);

    public partial List<InventoryGoodImportDto> ToDtoList(List<InventoryGood> src);
    public partial List<InventoryGood> ToEntityList(List<InventoryGoodImportDto> src);
}
