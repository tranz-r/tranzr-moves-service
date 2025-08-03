using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable(Db.Tables.Jobs);
        builder.HasKey(x => x.Id);
            
        builder.HasMany(x => x.InventoryItems)
            .WithOne(x => x.Job)
            .HasForeignKey(x => x.JobId)
            .IsRequired();
    }
}