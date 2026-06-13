using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TranzrMoves.Notifications.Infrastructure.Entities;

namespace TranzrMoves.Notifications.Infrastructure;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    public DbSet<CustomerMarketingPreference> CustomerMarketingPreferences => Set<CustomerMarketingPreference>();

    public DbSet<MarketingConsentEvent> MarketingConsentEvents => Set<MarketingConsentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
