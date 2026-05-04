using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        builder.ToTable(Db.Tables.DataProtectionKeys, Db.SCHEMA);
    }
}
