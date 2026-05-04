using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class QuoteAddressConfiguration : IEntityTypeConfiguration<QuoteAddress>
{
    public void Configure(EntityTypeBuilder<QuoteAddress> builder)
    {
        builder.ToTable(Db.Tables.QuoteAddresses, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Kind)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (QuoteAddressKind)Enum.Parse(typeof(QuoteAddressKind), v));

        builder.Property(x => x.Line1).IsRequired();
        builder.Property(x => x.PostCode).IsRequired();

        builder.HasIndex(x => x.QuoteId)
            .HasDatabaseName("IX_QuoteAddresses_QuoteId");
    }
}
