namespace TranzrMoves.Observability;

public sealed class TranzrObservabilityOptions
{
    public required string ServiceName { get; init; }

    public string? ServiceVersion { get; init; }

    public bool EnableAspNetCore { get; init; } = true;

    public bool EnableRedis { get; init; }

    public bool EnableRabbitMq { get; init; } = true;

    public bool ExportMetrics { get; init; } = true;

    public bool ExportTraces { get; init; } = true;
}
