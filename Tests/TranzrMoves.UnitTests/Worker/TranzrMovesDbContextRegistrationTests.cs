using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Infrastructure;
using TranzrMoves.Infrastructure.DependencyInjection;

namespace TranzrMoves.UnitTests.Worker;

public sealed class TranzrMovesDbContextRegistrationTests
{
    [Fact]
    public void AddTranzrMovesDatabase_WithMissingConnectionString_ThrowsAtRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(_ => SystemClock.Instance);
        services.AddSingleton<ITimeService, TimeService>();

        var act = () => services.AddTranzrMovesDatabase(new ConfigurationBuilder().Build());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Connection string 'TranzrMovesDatabaseConnection' is not configured*");
    }

    [Fact]
    public void AddTranzrMovesDatabase_WithConnectionString_ConfiguresNpgsqlProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TranzrMovesDatabaseConnection"] =
                    "Server=localhost;Port=5432;Database=tranzr;User Id=tranzr;Password=tranzr;"
            })
            .Build();

        var services = BuildServices(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranzrMovesDbContext>();

        db.Database.ProviderName.Should().Contain("Npgsql");
    }

    private static ServiceCollection BuildServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(_ => SystemClock.Instance);
        services.AddSingleton<ITimeService, TimeService>();
        services.AddTranzrMovesDatabase(configuration);
        return services;
    }
}
