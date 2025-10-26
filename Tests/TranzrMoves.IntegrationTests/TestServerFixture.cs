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

using Testcontainers.PostgreSql;

using TranzrMoves.Api;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure;
using TranzrMoves.Infrastructure.Interceptors;

using WireMock.Server;

namespace TranzrMoves.IntegrationTests;

public class TestServerFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
        private PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase($"testdb_{Guid.NewGuid()}")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        private IServiceScope _scope;

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
                foreach (var entry in args.NewItems)
                {
                    Console.WriteLine(entry.ToString());
                }
            };

            builder.ConfigureAppConfiguration(configBuilder =>
            {
                var defaults = new Dictionary<string, string?>
                {
                    ["STRIPE_API_KEY"] = "sk_test_51RUsSF4Eg1kVsmJlrD3DlxUxzsSW657e6vBJxps9TnQESbxAw3QzyOru91gWi3KGZPlknXGOawW7eeUduhtZoUQZ00XaJNJU9c",
                };

                configBuilder.AddInMemoryCollection(defaults);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<TranzrMovesDbContext>>();
                services.RemoveAll<DbConnection>();
                services.RemoveAll<IEmailService>();

                services.AddScoped<AuditableInterceptor>();
                services.AddScoped<IEmailService, LocalEmailService>();

                services.AddDbContext<TranzrMovesDbContext>((sp, options) =>
                    options.UseNpgsql(_postgres.GetConnectionString())
                        .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>()));
            });
        }

        public async Task ResetDatabaseStateAsync()
        {
            await _respawner.ResetAsync(_connection);
        }

        public async Task InitializeAsync()
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

            await DbContext.Database.EnsureDeletedAsync(); //Have a clean state
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
