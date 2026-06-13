using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Notifications.Application.Services;
using TranzrMoves.Notifications.Application.Telemetry;
using TranzrMoves.Notifications.Infrastructure.DependencyInjection;
using TranzrMoves.Notifications.Infrastructure.Repositories;

namespace TranzrMoves.Notifications.Application.DependencyInjection;

public static class NotificationsApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddScoped<IMarketingPreferenceRepository, MarketingPreferenceRepository>();
        services.AddScoped<IMarketingPreferenceService, MarketingPreferenceService>();
        services.AddSingleton<NotificationsMetrics>();
        return services;
    }

    public static IServiceCollection AddNotificationsConsentServices(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddNotificationsInfrastructure(configuration);
        services.AddNotificationsApplication();
        services.AddScoped<IMarketingPreferenceTokenService, MarketingPreferenceTokenService>();
        return services;
    }
}
