using Serilog;
using Stripe;
using Supabase;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

// Add services to the container.

    builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddHttpLogging(o => o.CombineLogs = true);
    builder.Services.AddHealthChecks();
    builder.Services.AddSingleton(new StripeClient(builder.Configuration["STRIPE_API_KEY"]));
    
    builder.Services.AddSingleton(s => new GetAddress.ApiKeys(builder.Configuration["ADDRESS_API_KEY"], 
        builder.Configuration["ADDRESS_ADMINISTRATION_KEY"]));
    builder.Services.AddHttpClient<GetAddress.Api>();

    
    builder.Services.AddSingleton( _ =>
    {
        var url = builder.Configuration["SUPABASE_URL"];
        var key = builder.Configuration["SUPABASE_KEY"];

        
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true,
        };

        return new Client(url, key, options);
    });

    var app = builder.Build();

// Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpLogging();
    app.UseHttpsRedirection();
    app.MapHealthChecks("/healthz");
    app.MapHealthChecks("/ready");
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.CloseAndFlush();
}