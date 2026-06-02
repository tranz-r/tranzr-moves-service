using Microsoft.EntityFrameworkCore;
using TranzrMoves.Notifications.Infrastructure.Constants;
using TranzrMoves.Notifications.Infrastructure.Entities;

namespace TranzrMoves.Notifications.Infrastructure;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(NotificationsDb.Schema);

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("NotificationDeliveries");
            entity.HasKey(x => x.MessageId);
            entity.Property(x => x.CorrelationId).HasMaxLength(256).IsRequired();
            entity.Property(x => x.TemplateKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ToEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ProviderMessageId).HasMaxLength(256);
            entity.Property(x => x.Error).HasMaxLength(4000);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });
    }
}
