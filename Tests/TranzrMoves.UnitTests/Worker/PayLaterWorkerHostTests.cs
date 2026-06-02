using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Stripe;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure;
using TranzrMoves.Infrastructure.DependencyInjection;
using TranzrMoves.Worker;
using TranzrMoves.Worker.HostedServices;

namespace TranzrMoves.UnitTests.Worker;

public sealed class PayLaterWorkerHostTests
{
    [Fact]
    public void AddPayLaterWorkerServices_Scheduler_IncludesRedisNotBalanceCollection()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        services.AddPayLaterWorkerServices(configuration, includeRedis: true, includeBalanceCollection: false);

        services.Should().Contain(d => d.ServiceType == typeof(IConnectionMultiplexer));
        services.Should().Contain(d => d.ServiceType == typeof(IQuoteRepository));
        services.Should().Contain(d => d.ServiceType == typeof(ICollectQuoteV2BalanceChargePublisher));
        services.Should().NotContain(d => d.ServiceType == typeof(IQuoteV2LaterBalanceCollectionService));
        services.Select(d => d.ServiceType).Should().NotContain(typeof(INotificationPublisher));
    }

    [Fact]
    public void AddPayLaterWorkerServices_Processor_IncludesBalanceCollectionNotRedis()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        services.AddPayLaterWorkerServices(configuration, includeRedis: false, includeBalanceCollection: true);

        services.Should().Contain(d => d.ServiceType == typeof(IQuoteV2LaterBalanceCollectionService));
        services.Should().Contain(d => d.ServiceType == typeof(IQuoteV2DepositBalanceCollectionService));
        services.Select(d => d.ServiceType).Should().Contain(typeof(IQuoteV2HostedCheckoutSessionService));
        services.Select(d => d.ServiceType).Should().Contain(typeof(INotificationPublisher));
        services.Select(d => d.ServiceType).Should().NotContain(typeof(IConnectionMultiplexer));
    }

    [Fact]
    public void Configure_Scheduler_RegistersListenerAndNotStripe()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Configuration.AddConfiguration(BuildConfiguration());
        builder.Configuration[WorkerHostConfiguration.RoleConfigurationKey] = nameof(WorkerRole.Scheduler);

        TranzrMovesWorkerHost.Configure(builder, WorkerRole.Scheduler);

        builder.Services.Should().Contain(d =>
            d.ImplementationType == typeof(BalanceChargeExpiryListener));
        builder.Services.Should().NotContain(d => d.ServiceType == typeof(StripeClient));
        builder.Services.Should().NotContain(d =>
            d.ServiceType == typeof(IQuoteV2LaterBalanceCollectionService));
    }

    [Fact]
    public void Configure_Processor_RegistersStripeAndBalanceCollectionNotListener()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Configuration.AddConfiguration(BuildConfiguration());
        builder.Configuration[WorkerHostConfiguration.RoleConfigurationKey] = nameof(WorkerRole.Processor);

        TranzrMovesWorkerHost.Configure(builder, WorkerRole.Processor);

        builder.Services.Should().Contain(d => d.ServiceType == typeof(StripeClient));
        builder.Services.Should().Contain(d =>
            d.ServiceType == typeof(IQuoteV2LaterBalanceCollectionService));
        builder.Services.Should().NotContain(d =>
            d.ImplementationType == typeof(BalanceChargeExpiryListener));
        builder.Services.Should().NotContain(d => d.ServiceType == typeof(IConnectionMultiplexer));
    }

    [Fact]
    public void Configure_Processor_ResolvesDbContextWithNpgsqlProvider()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Configuration.AddConfiguration(BuildConfiguration());
        builder.Configuration[WorkerHostConfiguration.RoleConfigurationKey] = nameof(WorkerRole.Processor);

        TranzrMovesWorkerHost.Configure(builder, WorkerRole.Processor);

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranzrMovesDbContext>();

        db.Database.ProviderName.Should().Contain("Npgsql");
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TranzrMovesDatabaseConnection"] =
                    "Server=localhost;Port=5432;Database=tranzr;User Id=tranzr;Password=tranzr;",
                ["ConnectionStrings:redis"] = "localhost:6379",
                ["ConnectionStrings:rabbitmq"] = "amqp://guest:guest@localhost:5672",
                ["STRIPE_API_KEY"] = "sk_test_placeholder",
                ["CHECKOUT_SESSION_SUCCESS_URL"] = "http://localhost:3000/success",
                ["CHECKOUT_SESSION_CANCEL_URL"] = "http://localhost:3000/cancel"
            })
            .Build();
}
