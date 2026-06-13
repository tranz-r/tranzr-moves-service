using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Notifications.Infrastructure.Constants;
using TranzrMoves.Notifications.Infrastructure.Entities;

namespace TranzrMoves.Notifications.Infrastructure.Configurations;

public sealed class CustomerMarketingPreferenceConfiguration
    : IEntityTypeConfiguration<CustomerMarketingPreference>
{
    public void Configure(EntityTypeBuilder<CustomerMarketingPreference> builder)
    {
        builder.ToTable("CustomerMarketingPreferences", NotificationsDb.Schema);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.EmailMarketingEnabled)
            .HasDefaultValue(false);

        builder.Property(x => x.SmsMarketingEnabled)
            .HasDefaultValue(false);
    }
}
