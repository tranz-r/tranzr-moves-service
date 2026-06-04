using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;

namespace TranzrMoves.Observability;

public static class HttpLoggingHostExtensions
{
    public static IServiceCollection AddTranzrHttpLogging(this IServiceCollection services)
    {
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.RequestQuery
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });

        return services;
    }

    public static IApplicationBuilder UseTranzrHttpLogging(this IApplicationBuilder app) =>
        app.UseWhen(
            context => !IsHealthCheckPath(context.Request.Path),
            branch => branch.UseHttpLogging());

    private static bool IsHealthCheckPath(PathString path) =>
        path.StartsWithSegments("/healthz") || path.StartsWithSegments("/ready");
}
