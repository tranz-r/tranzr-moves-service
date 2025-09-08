using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class LegalDocument : IAuditable
{
    public Guid Id { get; set; }
    public LegalDocumentType DocumentType { get; set; }
    public string BlobName { get; set; } = string.Empty; // Azure blob name
    public string ContainerName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty; // SHA256 hash of content
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int ContentLength { get; set; } // Content size in bytes
    public string ContentHash { get; set; } = string.Empty; // MD5 hash for integrity
    
    // Optimistic concurrency
    public uint RowVersion { get; set; }
    
    // Audit properties
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
}
