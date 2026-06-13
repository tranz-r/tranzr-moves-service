using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Notifications.Infrastructure.Constants;
using TranzrMoves.Notifications.Infrastructure.Entities;

namespace TranzrMoves.Notifications.Infrastructure.Configurations;

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries", NotificationsDb.Schema);
        builder.HasKey(x => x.MessageId);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.TemplateKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ToEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(256);

        builder.Property(x => x.Error)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
