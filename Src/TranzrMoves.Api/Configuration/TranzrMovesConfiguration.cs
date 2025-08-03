using Microsoft.EntityFrameworkCore;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Infrastructure;
using TranzrMoves.Infrastructure.Interceptors;

namespace TranzrMoves.Api.Configuration
{
    internal static class TranzrMovesConfiguration
    {
        internal static void ConfigureTranzrMovesServices(this IServiceCollection serviceCollection,
            IConfiguration configuration)
        {
            serviceCollection.ConfigureDatabase(configuration);
        }

        private static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<AuditableInterceptor>();

            string? dbConnectionString = configuration.GetConnectionString(Db.CONNECTION_STRING_NAME);
            services.AddDbContext<TranzrMovesDbContext>((sp, options) =>
                options.UseNpgsql(dbConnectionString)
                    .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>()));
        }
    }
}