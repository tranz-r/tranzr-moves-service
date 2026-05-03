using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class PricingConfiguration : IEntityTypeConfiguration<Pricing>
{
    public void Configure(EntityTypeBuilder<Pricing> builder)
    {
        builder.ToTable(Db.Tables.Pricings, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuoteType)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (QuoteType)Enum.Parse(typeof(QuoteType), v));

        builder.Property(x => x.ServiceLevel)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (ServiceLevel)Enum.Parse(typeof(ServiceLevel), v));

        builder.HasIndex(x => x.QuoteId)
            .HasDatabaseName("IX_Pricings_QuoteId");
    }
}
