using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class UserV2Configuration : IEntityTypeConfiguration<UserV2>
{
    public void Configure(EntityTypeBuilder<UserV2> builder)
    {
        builder.ToTable(Db.Tables.UsersV2, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.Addresses)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_UsersV2_Email").IsUnique();

        builder.HasIndex(x => x.SupabaseId)
            .HasDatabaseName("IX_UsersV2_SupabaseId")
            .IsUnique()
            .HasFilter("\"SupabaseId\" IS NOT NULL");
    }
}
