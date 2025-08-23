using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranzrMoves.Infrastructure;

namespace TranzrMoves.IntegrationTests;

public class TestingWebAppFactory : WebApplicationFactory<TranzrMoves.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var defaults = new Dictionary<string, string?>
            {
                ["MAPBOX_BASE_URL"] = "https://localhost/",
                ["STRIPE_API_KEY"] = "sk_test_123",
                ["ADDRESS_API_KEY"] = "test",
                ["ADDRESS_ADMINISTRATION_KEY"] = "test",
                ["SUPABASE_URL"] = "https://localhost/",
                ["SUPABASE_KEY"] = "anon",
                ["AWS:Region"] = "eu-west-2",
                ["AWS_ACCESS_KEY_ID"] = "test",
                ["AWS_SECRET_ACCESS_KEY"] = "test",
                ["ConnectionStrings:TranzrMovesDatabaseConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            };
            configBuilder.AddInMemoryCollection(defaults);
        });

        builder.ConfigureServices(services =>
        {
            // Replace Postgres DbContext with InMemory for tests
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TranzrMovesDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<TranzrMovesDbContext>(options =>
            {
                options.UseInMemoryDatabase("TranzrMovesTestDb");
            });

            // Build provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TranzrMovesDbContext>();
            db.Database.EnsureCreated();
        });
    }
}


