using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class AddressV2Configuration : IEntityTypeConfiguration<AddressV2>
{
    public void Configure(EntityTypeBuilder<AddressV2> builder)
    {
        builder.ToTable(Db.Tables.AddressesV2, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, x.Type })
            .IsUnique()
            .HasDatabaseName("IX_AddressesV2_UserId_Type");

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (AddressType)Enum.Parse(typeof(AddressType), v));

        builder.Property(x => x.Line1).IsRequired();
        builder.Property(x => x.PostCode).IsRequired();
    }
}
