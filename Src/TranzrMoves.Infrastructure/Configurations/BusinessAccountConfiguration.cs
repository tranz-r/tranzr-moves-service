using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class BusinessAccountConfiguration : IEntityTypeConfiguration<BusinessAccount>
{
    public void Configure(EntityTypeBuilder<BusinessAccount> builder)
    {
        builder.ToTable(Db.Tables.BusinessAccounts, Db.SCHEMA);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TradingName).HasMaxLength(200);
        builder.Property(x => x.BusinessEmail).IsRequired().HasMaxLength(320);
        builder.Property(x => x.BusinessPhone).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CompanyRegistrationNumber).HasMaxLength(50);
        builder.Property(x => x.VatNumber).HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BusinessAccountStatus)Enum.Parse(typeof(BusinessAccountStatus), v));

        builder.HasIndex(x => x.BusinessEmail)
            .IsUnique()
            .HasDatabaseName("IX_BusinessAccounts_BusinessEmail");

        builder.OwnsOne(x => x.BillingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Line1).IsRequired().HasMaxLength(500);
            addressBuilder.Property(a => a.PostCode).IsRequired().HasMaxLength(20);
            addressBuilder.Property(a => a.Line2).HasMaxLength(500);
            addressBuilder.Property(a => a.City).HasMaxLength(200);
            addressBuilder.Property(a => a.County).HasMaxLength(200);
            addressBuilder.Property(a => a.Country).HasMaxLength(200);
            addressBuilder.Property(a => a.FullAddress).HasMaxLength(1000);
            addressBuilder.Property(a => a.AddressNumber).HasMaxLength(50);
            addressBuilder.Property(a => a.Street).HasMaxLength(500);
            addressBuilder.Property(a => a.Neighborhood).HasMaxLength(200);
            addressBuilder.Property(a => a.District).HasMaxLength(200);
            addressBuilder.Property(a => a.Region).HasMaxLength(200);
            addressBuilder.Property(a => a.RegionCode).HasMaxLength(20);
            addressBuilder.Property(a => a.CountryCode).HasMaxLength(10);
            addressBuilder.Property(a => a.PlaceName).HasMaxLength(1000);
            addressBuilder.Property(a => a.Accuracy).HasMaxLength(50);
            addressBuilder.Property(a => a.MapboxId).HasMaxLength(200);
        });

        builder.HasMany(x => x.BusinessUsers)
            .WithOne(x => x.BusinessAccount)
            .HasForeignKey(x => x.BusinessAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
