using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class LegalDocumentMapper
{
    public partial LegalDocumentDto ToDto(LegalDocument document);
    public partial List<LegalDocumentDto> ToDtoList(List<LegalDocument> documents);
    
    [MapperIgnoreTarget(nameof(LegalDocument.Id))]
    [MapperIgnoreTarget(nameof(LegalDocument.RowVersion))]
    [MapperIgnoreTarget(nameof(LegalDocument.CreatedAt))]
    [MapperIgnoreTarget(nameof(LegalDocument.CreatedBy))]
    [MapperIgnoreTarget(nameof(LegalDocument.ModifiedAt))]
    [MapperIgnoreTarget(nameof(LegalDocument.ModifiedBy))]
    public partial LegalDocument ToEntity(LegalDocumentDto dto);
}
