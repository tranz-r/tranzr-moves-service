using TranzrMoves.Infrastructure.DependencyInjection;

namespace TranzrMoves.Api.Configuration
{
    internal static class TranzrMovesConfiguration
    {
        internal static void ConfigureTranzrMovesServices(this IServiceCollection serviceCollection,
            IConfiguration configuration)
        {
            serviceCollection.AddTranzrMovesDatabase(configuration);
        }
    }
}
