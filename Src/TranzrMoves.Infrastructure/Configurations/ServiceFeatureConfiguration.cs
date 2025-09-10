using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class ServiceFeatureConfiguration : IEntityTypeConfiguration<ServiceFeature>
{
    public void Configure(EntityTypeBuilder<ServiceFeature> builder)
    {
        builder.ToTable(Db.Tables.ServiceFeatures);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceLevel).IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (ServiceLevel)Enum.Parse(typeof(ServiceLevel), v));
        
        builder.Property(x => x.Text).IsRequired();

        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveTo);
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();

        builder.HasIndex(x => new { x.ServiceLevel, x.IsActive, x.EffectiveFrom, x.EffectiveTo, x.DisplayOrder });
        
        // Concurrency Token
        builder.Property(b => b.Version)
            .IsRowVersion()
            .HasColumnName("xmin"); // Map to PostgreSQL xmin system column
    }
}