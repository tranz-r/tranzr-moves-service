using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public class CustomerQuoteConfiguration : IEntityTypeConfiguration<CustomerQuote>
{
    public void Configure(EntityTypeBuilder<CustomerQuote> builder)
    {
        builder.ToTable(Db.Tables.CustomerQuotes);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, JobId = x.QuoteId }).IsUnique();

        builder.HasOne(uj => uj.User)
            .WithMany(u => u.CustomerQuotes)
            .HasForeignKey(uj => uj.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(uj => uj.Quote)
            .WithMany(j => j.CustomerQuotes)
            .HasForeignKey(uj => uj.QuoteId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}