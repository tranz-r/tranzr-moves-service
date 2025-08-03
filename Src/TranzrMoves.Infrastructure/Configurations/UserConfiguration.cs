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
                    v => v.ToString(),
                    v => (Role?)Enum.Parse(typeof(Role), v));
            
            builder.HasOne(x => x.Address)
                .WithOne(x => x.User)
                .HasForeignKey<Address>(a => a.UserId)
                .IsRequired();

            builder.HasMany(x => x.Jobs)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId).IsRequired();
        }
    }
}