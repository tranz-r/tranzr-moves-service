using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public class QuoteV2Configuration : IEntityTypeConfiguration<QuoteV2>
{
    public void Configure(EntityTypeBuilder<QuoteV2> builder)
    {
        builder.ToTable(Db.Tables.QuotesV2, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId).IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (QuoteType)Enum.Parse(typeof(QuoteType), v));

        builder.Property(x => x.PaymentStatus)
            .HasConversion(
                v => v == null ? null : v.ToString(),
                v => (PaymentStatus)Enum.Parse(typeof(PaymentStatus), v!));

        builder.Property(x => x.ServiceTier)
            .HasConversion(
                v => v == null ? null : v.ToString(),
                v => (ServiceLevel?)Enum.Parse(typeof(ServiceLevel), v!));

        builder.Property(x => x.VanType)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (VanType)Enum.Parse(typeof(VanType), v));

        builder.Property(x => x.VanCapacityStatus)
            .HasConversion(
                v => v == null ? null : v.ToString(),
                v => (VanCapacityStatus?)Enum.Parse(typeof(VanCapacityStatus), v!));

        builder.Property(x => x.StepsCompleted)
            .IsRequired()
            .HasConversion<long>();

        builder.Property(x => x.QuoteReference).IsRequired();
        builder.Property(x => x.OriginDestinationRoute).HasColumnType("text");

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Quotes)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Addresses)
            .WithOne(x => x.Quote)
            .HasForeignKey(x => x.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Pricings)
            .WithOne(x => x.Quote)
            .HasForeignKey(x => x.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Quote)
            .HasForeignKey(x => x.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.InventoryItems)
            .WithOne(x => x.Quote)
            .HasForeignKey(x => x.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Schedule)
            .WithOne(x => x.Quote)
            .HasForeignKey<Schedule>(x => x.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_QuotesV2_CreatedAt");

        builder.HasIndex(x => x.QuoteReference)
            .HasDatabaseName("IX_QuotesV2_QuoteReference");

        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_QuotesV2_SessionId");

        builder.HasIndex(x => x.CustomerId)
            .HasDatabaseName("IX_QuotesV2_UserId");

        builder.Property(x => x.Version).IsRowVersion();
    }
}
