using TranzrMoves.Infrastructure.DependencyInjection;
using TranzrMoves.Notifications.Application.DependencyInjection;

namespace TranzrMoves.Api.Configuration;

internal static class TranzrMovesConfiguration
{
    internal static void ConfigureTranzrMovesServices(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddTranzrMovesDatabase(configuration);
        serviceCollection.AddNotificationsConsentServices(configuration);
    }
}
