using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable(Db.Tables.Quotes);
        builder.HasKey(x => x.Id);
        
        // Session Management
        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        
        // Core Properties
        builder.Property(x => x.VanType).IsRequired();
        builder.Property(x => x.DriverCount).IsRequired();
        builder.Property(x => x.QuoteReference).IsRequired();
        
        // Address Properties
        builder.OwnsOne(x => x.Origin, origin =>
        {
            origin.Property(p => p.Line1).IsRequired().HasMaxLength(200);
            origin.Property(p => p.Line2).HasMaxLength(200);
            origin.Property(p => p.City).HasMaxLength(100);
            origin.Property(p => p.PostCode).IsRequired().HasMaxLength(10);
            origin.Property(p => p.Country).HasMaxLength(100);
        });
        
        builder.OwnsOne(x => x.Destination, destination =>
        {
            destination.Property(p => p.Line1).IsRequired().HasMaxLength(200);
            destination.Property(p => p.Line2).HasMaxLength(200);
            destination.Property(p => p.City).HasMaxLength(100);
            destination.Property(p => p.PostCode).IsRequired().HasMaxLength(10);
            destination.Property(p => p.Country).HasMaxLength(100);
        });
        
        // Schedule Properties
        builder.Property(x => x.CollectionDate);
        builder.Property(x => x.DeliveryDate);
        builder.Property(x => x.Hours);
        builder.Property(x => x.FlexibleTime);
        builder.Property(x => x.TimeSlot);
        
        // Pricing Properties
        builder.Property(x => x.TotalCost);
        
        // Inventory Items
        builder.OwnsMany(c => c.InventoryItems, inventoryItem =>
        {
            inventoryItem.ToTable(Db.Tables.InventoryItems);
            inventoryItem.WithOwner().HasForeignKey(e => e.JobId);
        });
    }
}