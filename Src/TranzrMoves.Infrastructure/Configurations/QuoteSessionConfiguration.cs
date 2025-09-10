using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public class QuoteSessionConfiguration : IEntityTypeConfiguration<QuoteSession>
{
    public void Configure(EntityTypeBuilder<QuoteSession> builder)
    {
        builder.ToTable(Db.Tables.QuoteSessions);
        builder.HasKey(x => x.SessionId);
        
        // Session Management
        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.ExpiresUtc);
        
        // Customer Info is now stored per quote, not in session
        
        // Quotes (One-to-Many relationship)
        builder.HasMany(x => x.Quotes)
            .WithOne()
            .HasForeignKey(q => q.SessionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}


