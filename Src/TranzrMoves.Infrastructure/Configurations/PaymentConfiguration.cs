using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable(Db.Tables.Payments, Db.SCHEMA);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (StripePaymentStatus)Enum.Parse(typeof(StripePaymentStatus), v));

        builder.Property(x => x.PaymentType)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (PaymentType)Enum.Parse(typeof(PaymentType), v));

        builder.HasIndex(x => x.QuoteId)
            .HasDatabaseName("IX_Payments_QuoteId");
    }
}
