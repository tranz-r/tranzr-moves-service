using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public class DriverJobConfiguration : IEntityTypeConfiguration<DriverJob>
{
    public void Configure(EntityTypeBuilder<DriverJob> builder)
    {
        builder.ToTable(Db.Tables.DriverJobs);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, x.JobId }).IsUnique();

        builder.HasOne(uj => uj.User)
            .WithMany(u => u.DriverJobs)
            .HasForeignKey(uj => uj.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(uj => uj.Job)
            .WithMany(j => j.DriverJobs)
            .HasForeignKey(uj => uj.JobId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}