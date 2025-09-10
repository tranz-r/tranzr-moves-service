using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public record CreateLegalDocumentRequest(
    LegalDocumentType DocumentType,
    string MarkdownContent);

public record CreateLegalDocumentResponse(
    Guid Id,
    LegalDocumentType DocumentType,
    string Version,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo);

public record GetLegalDocumentRequest(
    LegalDocumentType DocumentType,
    DateTimeOffset? AsOfDate = null);

public record GetLegalDocumentResponse(
    Guid Id,
    LegalDocumentType DocumentType,
    string MarkdownContent,
    string Version,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public record LegalDocumentDto
{
    public Guid Id { get; init; }
    public LegalDocumentType DocumentType { get; init; }
    public string BlobName { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public DateTimeOffset EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public bool IsActive { get; init; }
    public int ContentLength { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public uint RowVersion { get; init; }
}

public record GetLegalDocumentHistoryResponse(
    List<LegalDocumentDto> Documents);
