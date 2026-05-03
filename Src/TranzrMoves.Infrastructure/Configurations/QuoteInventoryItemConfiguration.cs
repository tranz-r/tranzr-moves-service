using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class QuoteInventoryItemConfiguration : IEntityTypeConfiguration<QuoteInventoryItem>
{
    public void Configure(EntityTypeBuilder<QuoteInventoryItem> builder)
    {
        builder.ToTable(Db.Tables.InventoryItemsV2, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired();

        builder.HasIndex(x => x.QuoteId)
            .HasDatabaseName("IX_InventoryItemsV2_QuoteId");
    }
}
