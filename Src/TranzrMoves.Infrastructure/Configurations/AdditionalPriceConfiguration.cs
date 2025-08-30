using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class AdditionalPriceConfiguration : IEntityTypeConfiguration<AdditionalPrice>
{
    public void Configure(EntityTypeBuilder<AdditionalPrice> builder)
    {
        builder.ToTable(Db.Tables.AdditionalPrices);
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Type).IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (AdditionalPriceType)Enum.Parse(typeof(AdditionalPriceType), v));

        builder.Property(x => x.Price).IsRequired();

        builder.Property(x => x.CurrencyCode).HasMaxLength(3).HasDefaultValue("GBP").IsRequired();

        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveTo);
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();

        builder.HasIndex(x => new { x.Type, x.IsActive, x.EffectiveFrom, x.EffectiveTo });
        
        // Concurrency Token
        builder.Property(b => b.Version)
            .IsRowVersion()
            .HasColumnName("xmin");
    }
}