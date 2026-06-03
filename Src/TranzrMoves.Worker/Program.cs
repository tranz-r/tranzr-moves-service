using Serilog;
using TranzrMoves.Observability;
using TranzrMoves.Worker;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .ConfigureTranzrMovesSerilog()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog();

    var role = WorkerHostConfiguration.GetWorkerRole(builder.Configuration, builder.Environment);
    var serviceName = WorkerHostConfiguration.GetObservabilityServiceName(role);
    Log.Information("Starting TranzrMoves.Worker with role {WorkerRole}", role);

    builder.AddTranzrMovesObservability(new TranzrObservabilityOptions
    {
        ServiceName = serviceName,
        EnableAspNetCore = false,
        EnableRedis = role is WorkerRole.Scheduler or WorkerRole.All,
        EnableRabbitMq = true,
    });

    TranzrMovesWorkerHost.Configure(builder, role);

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker failed to start or stopped due to an unhandled exception.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
