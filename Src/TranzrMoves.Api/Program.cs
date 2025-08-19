using Serilog;
using Stripe;
using Supabase;
using TranzrMoves.Api.Configuration;
using TranzrMoves.Api.Services;
using TranzrMoves.Application.DependencyInjection;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.DependencyInjection;
using TranzrMoves.Infrastructure.Services;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

// Add services to the container.
    var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
            policy  =>
            {
                policy.WithOrigins(new[]{"http://localhost:3000", "http://localhost:3001"})
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
    });
    
    builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddHttpLogging(o => o.CombineLogs = true);
    builder.Services.AddHealthChecks();
    builder.Services.AddSingleton(new StripeClient(builder.Configuration["STRIPE_API_KEY"]));
    
    builder.Services.AddSingleton(s => new GetAddress.ApiKeys(builder.Configuration["ADDRESS_API_KEY"], 
        builder.Configuration["ADDRESS_ADMINISTRATION_KEY"]));
    builder.Services.AddHttpClient<GetAddress.Api>();
    
    // Register email service
    builder.Services.AddScoped<IEmailService, EmailService>();
    
    builder.Services.AddHttpClient<IMapBoxService, MapBoxService>(
        client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["MAPBOX_BASE_URL"]);
        });
    
    builder.Services.ConfigureTranzrMovesServices(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();

    
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
    app.UseCors(MyAllowSpecificOrigins);
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.CloseAndFlush();
}