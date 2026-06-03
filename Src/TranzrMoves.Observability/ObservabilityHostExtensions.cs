using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;

namespace TranzrMoves.Observability;

public static class ObservabilityHostExtensions
{
    public static TBuilder AddTranzrMovesObservability<TBuilder>(
        this TBuilder builder,
        TranzrObservabilityOptions options)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ServiceName);

        ConfigureDefaultPropagators();

        var openTelemetry = builder.Services.AddOpenTelemetry();
        openTelemetry.ConfigureResource(resource =>
            resource.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion ?? "1.0.0"));

        if (options.ExportTraces)
        {
            openTelemetry.WithTracing(tracing =>
            {
                if (options.EnableAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation();
                }

                tracing.AddHttpClientInstrumentation();

                tracing.AddEntityFrameworkCoreInstrumentation();

                if (options.EnableRedis)
                {
                    tracing.AddRedisInstrumentation();
                }

                if (options.EnableRabbitMq)
                {
                    tracing.AddRabbitMQInstrumentation();
                }

                tracing.AddSource("Wolverine");
            });
        }

        if (options.ExportMetrics)
        {
            openTelemetry.WithMetrics(metrics =>
            {
                if (options.EnableAspNetCore)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddMeter("Wolverine*");
            });
        }

        if (IsOtlpExportEnabled(builder.Configuration))
        {
            openTelemetry.UseOtlpExporter();
        }

        return builder;
    }

    public static LoggerConfiguration ConfigureTranzrMovesSerilog(
        this LoggerConfiguration loggerConfiguration) =>
        loggerConfiguration.Enrich.WithSpan();

    private static void ConfigureDefaultPropagators()
    {
        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
        [
            new TraceContextPropagator(),
            new BaggagePropagator()
        ]));
    }

    private static bool IsOtlpExportEnabled(IConfiguration configuration)
    {
        if (string.Equals(
                configuration["OTEL_SDK_DISABLED"],
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"])
               || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
    }
}
