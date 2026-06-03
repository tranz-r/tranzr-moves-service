using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using TranzrMoves.Notifications.Application.Handlers;
using TranzrMoves.Notifications.Infrastructure.DependencyInjection;
using TranzrMoves.Observability;
using Wolverine;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .ConfigureTranzrMovesSerilog()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.AddTranzrMovesObservability(new TranzrObservabilityOptions
    {
        ServiceName = "tranzr-moves-notifications",
        EnableAspNetCore = true,
        EnableRabbitMq = true,
    });

    builder.Services.AddNotificationsInfrastructure(builder.Configuration);
    builder.Services.AddHealthChecks();

    builder.Host.UseWolverine(opts =>
    {
        opts.ServiceName = "tranzr-moves-notifications";
        opts.ConfigureNotificationsMessaging(builder.Configuration, includeConsumer: true);
        opts.IncludeNotificationsHandlers(typeof(SendNotificationHandler).Assembly);
    });

    var app = builder.Build();

    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        Predicate = _ => true
    });
    app.MapGet("/", () => Results.Ok(new { service = "TranzrMoves.Notifications" }));

    Log.Information("Starting TranzrMoves.Notifications");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notifications host failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
