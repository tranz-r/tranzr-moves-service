using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Notifications.Contracts;
using TranzrMoves.Notifications.Infrastructure.Constants;
using TranzrMoves.Notifications.Infrastructure.Entities;

namespace TranzrMoves.Notifications.Infrastructure.Configurations;

public sealed class MarketingConsentEventConfiguration : IEntityTypeConfiguration<MarketingConsentEvent>
{
    public void Configure(EntityTypeBuilder<MarketingConsentEvent> builder)
    {
        builder.ToTable("MarketingConsentEvents", NotificationsDb.Schema);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.Channel)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => (MarketingConsentChannel)Enum.Parse(typeof(MarketingConsentChannel), v));

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => (MarketingConsentEventType)Enum.Parse(typeof(MarketingConsentEventType), v));

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion(
                v => v.ToString(),
                v => (MarketingConsentSource)Enum.Parse(typeof(MarketingConsentSource), v));

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.CustomerMarketingPreferenceId);
        builder.HasIndex(x => x.OccurredAt);

        builder.HasOne(x => x.Preference)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.CustomerMarketingPreferenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
