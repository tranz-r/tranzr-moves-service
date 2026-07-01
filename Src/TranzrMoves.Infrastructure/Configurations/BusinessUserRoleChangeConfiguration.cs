using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class BusinessUserRoleChangeConfiguration : IEntityTypeConfiguration<BusinessUserRoleChange>
{
    public void Configure(EntityTypeBuilder<BusinessUserRoleChange> builder)
    {
        builder.ToTable(Db.Tables.BusinessUserRoleChanges, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessAccountId).IsRequired();
        builder.Property(x => x.TargetBusinessUserId).IsRequired();
        builder.Property(x => x.ChangedByBusinessUserId).IsRequired();

        builder.Property(x => x.FromRole)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BusinessUserRole)Enum.Parse(typeof(BusinessUserRole), v));

        builder.Property(x => x.ToRole)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BusinessUserRole)Enum.Parse(typeof(BusinessUserRole), v));

        builder.Property(x => x.ChangeType)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (RoleChangeType)Enum.Parse(typeof(RoleChangeType), v));

        builder.HasIndex(x => x.BusinessAccountId)
            .HasDatabaseName("IX_BusinessUserRoleChanges_BusinessAccountId");

        builder.HasIndex(x => x.TargetBusinessUserId)
            .HasDatabaseName("IX_BusinessUserRoleChanges_TargetBusinessUserId");
    }
}
