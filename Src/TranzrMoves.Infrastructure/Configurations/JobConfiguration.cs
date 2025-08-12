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

        builder.OwnsOne(c => c.Origin, origin =>
        {
            origin.Property(p => p.AddressLine1).IsRequired();
            origin.Property(p => p.City).IsRequired();
            origin.Property(p => p.PostCode).IsRequired();
            origin.Property(p => p.AddressLine2).IsRequired(false);
            origin.Property(p => p.Country).IsRequired(false);
        });
        
        builder.Navigation(c => c.Origin).IsRequired();
        
        builder.OwnsOne(c => c.Destination, destination =>
        {
            destination.Property(p => p.AddressLine1).IsRequired();
            destination.Property(p => p.City).IsRequired();
            destination.Property(p => p.PostCode).IsRequired();
            destination.Property(p => p.AddressLine2).IsRequired(false);
            destination.Property(p => p.Country).IsRequired(false);
        });
        
        builder.Navigation(c => c.Destination).IsRequired();
        builder.OwnsOne(c => c.Cost);
        builder.Navigation(c => c.Cost).IsRequired(false);
        
        builder.OwnsMany(c => c.InventoryItems, inventoryItem =>
        {
            inventoryItem.ToTable(Db.Tables.InventoryItems);
            inventoryItem.WithOwner().HasForeignKey(e => e.JobId);
        });
    }
}