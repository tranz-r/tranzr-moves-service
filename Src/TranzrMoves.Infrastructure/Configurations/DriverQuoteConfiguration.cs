using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public class DriverQuoteConfiguration : IEntityTypeConfiguration<DriverQuote>
{
    public void Configure(EntityTypeBuilder<DriverQuote> builder)
    {
        builder.ToTable(Db.Tables.DriverQuotes);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, JobId = x.QuoteId }).IsUnique();

        builder.HasOne(uj => uj.User)
            .WithMany(u => u.DriverQuotes)
            .HasForeignKey(uj => uj.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(uj => uj.Quote)
            .WithMany(j => j.DriverQuotes)
            .HasForeignKey(uj => uj.QuoteId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}