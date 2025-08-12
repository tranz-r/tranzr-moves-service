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

            builder.HasMany(x => x.CustomerJobs)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .IsRequired(false);
            
            builder.HasMany(x => x.DriverJobs)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .IsRequired(false);
        }
    }
}