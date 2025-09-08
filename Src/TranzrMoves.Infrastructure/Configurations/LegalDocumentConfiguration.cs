using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument>
{
    public void Configure(EntityTypeBuilder<LegalDocument> builder)
    {
        builder.ToTable(Db.Tables.LegalDocuments);
        builder.HasKey(d => d.Id);
        
        builder.Property(x => x.DocumentType).IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (LegalDocumentType)Enum.Parse(typeof(LegalDocumentType), v));

        builder.Property(d => d.BlobName).IsRequired();

        builder.Property(d => d.ContainerName).IsRequired();

        builder.Property(d => d.Version).IsRequired();

        builder.Property(d => d.EffectiveFrom).IsRequired();

        builder.Property(d => d.EffectiveTo);

        builder.Property(d => d.IsActive).IsRequired();

        builder.Property(d => d.ContentLength).IsRequired();

        builder.Property(d => d.ContentHash).IsRequired();

        // Optimistic concurrency
        builder.Property(b => b.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin"); // Map to PostgreSQL xmin system column

        // Indexes for performance
        builder.HasIndex(d => new { d.DocumentType, d.EffectiveFrom, d.EffectiveTo })
            .HasDatabaseName("IX_LegalDocuments_DocumentType_EffectiveDates");

        builder.HasIndex(d => new { d.DocumentType, d.IsActive })
            .HasDatabaseName("IX_LegalDocuments_DocumentType_IsActive");
    }
}
