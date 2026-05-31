using Serilog;
using TranzrMoves.Worker;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog();

    var role = WorkerHostConfiguration.GetWorkerRole(builder.Configuration, builder.Environment);
    Log.Information("Starting TranzrMoves.Worker with role {WorkerRole}", role);

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
