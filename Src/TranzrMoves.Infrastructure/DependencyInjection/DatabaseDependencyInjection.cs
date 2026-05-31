using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Infrastructure.Interceptors;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class DatabaseDependencyInjection
{
    public static IServiceCollection AddTranzrMovesDatabase(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditableInterceptor>();

        var dbConnectionString = configuration.GetConnectionString(Db.CONNECTION_STRING_NAME);
        if (string.IsNullOrWhiteSpace(dbConnectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{Db.CONNECTION_STRING_NAME}' is not configured.");
        }

        services.AddDbContext<TranzrMovesDbContext>((sp, options) =>
            options.UseNpgsql(dbConnectionString, x =>
                {
                    x.MigrationsHistoryTable("__MigrationHistory", Db.SCHEMA);
                    x.UseNodaTime();
                })
                .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>()));

        services.AddDataProtection()
            .SetApplicationName("TranzrMoves")
            .PersistKeysToDbContext<TranzrMovesDbContext>();

        return services;
    }
}
