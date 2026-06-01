using System.Data.Common;
using AutoBogus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using StackExchange.Redis;
using Stripe;
using Testcontainers.PostgreSql;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure;
using TranzrMoves.Infrastructure.Interceptors;
using TranzrMoves.IntegrationTests.Helpers;
using TranzrMoves.IntegrationTests.TestDoubles;
using WireMock.Server;

namespace TranzrMoves.IntegrationTests.Fixtures;

public class TestServerFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase($"testdb_{Guid.NewGuid()}")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private IServiceScope? _scope;

    private DbConnection _connection = default!;
    private Respawner _respawner = default!;
    internal WireMockServer? WireMockServer;

    public HttpClient? HttpClient { get; private set; }

    public TranzrMovesDbContext? DbContext { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        WireMockServer = WireMockServer.StartWithAdminInterface();
        WireMockServer.LogEntriesChanged += (sender, args) =>
        {
            if (args.NewItems != null)
            {
                foreach (var entry in args.NewItems)
                {
                    Console.WriteLine(entry.ToString());
                }
            }
        };

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddUserSecrets(typeof(TestServerFixture).Assembly, optional: true);
            configBuilder.AddEnvironmentVariables();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:redis"] = "127.0.0.1:6379,abortConnect=false,connectTimeout=100",
                ["COMMUNICATION_SERVICES_CONNECTION_STRING"] =
                    "endpoint=https://test.communication.azure.com/;accesskey=dGVzdA==",
                ["TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2"] = "whsec_test"
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            var stripeApiKey = PayLaterStripeTestHelper.RequireApiKey(context.Configuration);
            services.RemoveAll<StripeClient>();
            services.AddSingleton(_ => new StripeClient(stripeApiKey));

            services.RemoveAll<DbContextOptions<TranzrMovesDbContext>>();
            services.RemoveAll<DbConnection>();
            services.RemoveAll<IEmailService>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IBalanceChargeScheduler>();
            services.RemoveAll<ICollectQuoteV2BalanceChargePublisher>();

            services.AddScoped<AuditableInterceptor>();
            services.AddScoped<IEmailService, LocalEmailService>();
            services.AddSingleton<IBalanceChargeScheduler, NoOpBalanceChargeScheduler>();
            services.AddScoped<ICollectQuoteV2BalanceChargePublisher, DirectCollectQuoteV2BalanceChargePublisher>();

            services.AddDbContext<TranzrMovesDbContext>((sp, options) =>
                options.UseNpgsql(_postgres.GetConnectionString(), npgsql =>
                    {
                        npgsql.MigrationsHistoryTable("__MigrationHistory", Db.SCHEMA);
                        npgsql.UseNodaTime();
                    })
                    .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>()));
        });
    }

    public async Task ResetDatabaseStateAsync()
    {
        await _respawner.ResetAsync(_connection);
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await SetUpdateBaseAsync();

        _connection = new NpgsqlConnection(_postgres.GetConnectionString());
        HttpClient = CreateClient();

        await InitializeRespawner();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private async Task InitializeRespawner()
    {
        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection,
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres });
    }

    private async Task SetUpdateBaseAsync()
    {
        _scope = Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<TranzrMovesDbContext>();

        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.MigrateAsync();

        await ConfigureAutoFakerAsync();
    }

    private Task ConfigureAutoFakerAsync()
    {
        AutoFaker.Configure(builder =>
        {
            builder.WithRecursiveDepth(0);
        });

        return Task.CompletedTask;
    }
}
