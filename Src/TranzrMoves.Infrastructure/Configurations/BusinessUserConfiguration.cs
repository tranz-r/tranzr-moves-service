using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class BusinessUserConfiguration : IEntityTypeConfiguration<BusinessUser>
{
    public void Configure(EntityTypeBuilder<BusinessUser> builder)
    {
        builder.ToTable(Db.Tables.BusinessUsers, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BusinessUserRole)Enum.Parse(typeof(BusinessUserRole), v));

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BusinessUserStatus)Enum.Parse(typeof(BusinessUserStatus), v));

        builder.Property(x => x.CreatedByBusinessUserId);

        builder.Property(x => x.UpdatedByBusinessUserId);

        builder.Property(x => x.InvitationExpiresAt);

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("IX_BusinessUsers_UserId");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
