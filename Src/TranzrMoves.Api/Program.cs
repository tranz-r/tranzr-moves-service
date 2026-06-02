using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using Stripe;
using Supabase;
using Swashbuckle.AspNetCore.SwaggerGen;
using TranzrMoves.Api.Configuration;
using TranzrMoves.Application.DependencyInjection;
using TranzrMoves.Application.Services;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Infrastructure.DependencyInjection;
using TranzrMoves.Infrastructure.Helper;
using TranzrMoves.Infrastructure.Services;
using Wolverine;

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

    var configureNodaTime = new Action<JsonSerializerOptions>(opts =>
    {
        opts.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    });

    builder.Services.Configure<JsonOptions>(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        configureNodaTime(options.JsonSerializerOptions);
    });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddMvc().AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            configureNodaTime(options.JsonSerializerOptions);
        });

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHttpLogging(o => o.CombineLogs = true);
    builder.Services.AddHealthChecks();
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton(new StripeClient(builder.Configuration["STRIPE_API_KEY"]));

    //Find a way to move this registration to the application layer.
    builder.Services.AddHttpClient<IMapBoxService, MapBoxService>(
        client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["MAPBOX_BASE_URL"]!);
        });

    builder.Services.ConfigureTranzrMovesServices(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Host.UseWolverine(opts =>
            opts.ConfigureNotificationsPublisher(builder.Configuration));
    }

    builder.Services.AddSingleton(_ =>
    {
        var url = builder.Configuration["SUPABASE_URL"];
        var key = builder.Configuration["SUPABASE_KEY"];

        var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<Program>();

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
        {
            logger.LogCritical("Missing configuration for supabase url or key");
            throw new ArgumentException("Missing configuration for superbase");
        }

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true,
        };

        return new Client(url, key, options);
    });

    var app = builder.Build();
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    await app.Services.SeedAsync();

    // Configure the HTTP request pipeline.
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });

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
    Log.Fatal(ex, "Application failed to start or stopped due to an unhandled exception.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

namespace TranzrMoves.Api
{
    public partial class Program { }
}
