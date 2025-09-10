using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class RateCardConfiguration : IEntityTypeConfiguration<RateCard>
{
    public void Configure(EntityTypeBuilder<RateCard> builder)
    {
        builder.ToTable(Db.Tables.RateCards);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Movers).IsRequired();
        builder.Property(x => x.ServiceLevel).IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (ServiceLevel)Enum.Parse(typeof(ServiceLevel), v));

        builder.Property(x => x.BaseBlockHours).IsRequired();
        builder.Property(x => x.BaseBlockPrice).IsRequired();
        builder.Property(x => x.HourlyRateAfter).IsRequired();

        builder.Property(x => x.CurrencyCode).HasMaxLength(3).HasDefaultValue("GBP").IsRequired();

        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveTo);
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();

        builder.HasIndex(x => new { x.Movers, x.ServiceLevel, x.IsActive, x.EffectiveFrom, x.EffectiveTo });
        
        // Concurrency Token
        builder.Property(b => b.Version)
            .IsRowVersion()
            .HasColumnName("xmin"); // Map to PostgreSQL xmin system column
    }
}