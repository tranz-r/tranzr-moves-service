using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable(Db.Tables.Users);
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Role).IsRequired(false)
                .HasConversion(
                    v => v == null ? null : v.ToString(),
                    v => string.IsNullOrEmpty(v) ? null : (Role?)Enum.Parse(typeof(Role), v));

            builder.OwnsOne(x => x.BillingAddress);
            builder.Navigation(x => x.BillingAddress).IsRequired();

            builder.Property(x => x.Email).IsRequired();

            builder.HasMany(x => x.CustomerQuotes)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .IsRequired(false);

            builder.HasMany(x => x.DriverQuotes)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .IsRequired(false);

            builder.HasIndex(x => new { x.Email }).IsUnique();

            // Performance indexes for admin dashboard
            builder.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("IX_Users_CreatedAt");

            builder.HasIndex(x => new { x.Role, x.CreatedAt })
                .HasDatabaseName("IX_Users_Role_CreatedAt");
        }
    }
}
