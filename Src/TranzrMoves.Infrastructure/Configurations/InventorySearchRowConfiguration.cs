using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class InventorySearchRowConfiguration : IEntityTypeConfiguration<InventorySearchRow>
{
    public void Configure(EntityTypeBuilder<InventorySearchRow> builder)
    {
        builder.HasNoKey();
        builder.ToView(null);
    }
}
