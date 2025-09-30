using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Stripe;
using Supabase;
using TranzrMoves.Api.Configuration;
using TranzrMoves.Application.DependencyInjection;
using TranzrMoves.Domain.Constants;
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
    builder.Services.AddCors(options =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        options.AddPolicy(Cors.TranzrMovesCorsPolicy,
            policy =>
            {
                policy.WithOrigins(allowedOrigins ?? [])
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
    });

    builder.Services.Configure<JsonOptions>(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

    builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddHttpLogging(o => o.CombineLogs = true);
    builder.Services.AddHealthChecks();
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton(new StripeClient(builder.Configuration["STRIPE_API_KEY"]));

    builder.Services.AddSingleton(s => new GetAddress.ApiKeys(builder.Configuration["ADDRESS_API_KEY"],
        builder.Configuration["ADDRESS_ADMINISTRATION_KEY"]));
    builder.Services.AddHttpClient<GetAddress.Api>();

    // Email service is handled by IAwsEmailService in Infrastructure layer

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
    app.UseCors(Cors.TranzrMovesCorsPolicy);
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.CloseAndFlush();
}

namespace TranzrMoves.Api
{
    public partial class Program { }
}
