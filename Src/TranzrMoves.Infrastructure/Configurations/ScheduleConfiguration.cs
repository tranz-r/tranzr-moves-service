using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable(Db.Tables.Schedules, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TimeSlot)
            .HasConversion(
                v => v == null ? null : v.ToString(),
                v => (TimeSlot?)Enum.Parse(typeof(TimeSlot), v!));

        builder.HasIndex(x => x.QuoteId)
            .IsUnique()
            .HasDatabaseName("IX_Schedules_QuoteId");
    }
}
