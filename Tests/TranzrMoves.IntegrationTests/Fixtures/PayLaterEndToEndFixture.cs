using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NodaTime;
using StackExchange.Redis;
using Stripe;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure;
using TranzrMoves.IntegrationTests.Helpers;
using TranzrMoves.IntegrationTests.TestDoubles;
using TranzrMoves.Worker;

namespace TranzrMoves.IntegrationTests.Fixtures;

/// <summary>
/// Docker-based fixture: Redis expiry → scheduler publish → RabbitMQ → processor → Postgres + Stripe test mode.
/// Uses a single worker host with <see cref="WorkerRole.All"/> (Development) so Wolverine outbox/inbox
/// and Redis listener run in-process, matching local dev while still using real containers.
/// Requires Docker and STRIPE_API_KEY (sk_test_...) via user secrets or environment variables.
/// </summary>
public sealed class PayLaterEndToEndFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase($"paylater_e2e_{Guid.NewGuid():N}")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .WithCommand("redis-server", "--notify-keyspace-events", "Ex")
        .Build();

    private IHost? _workerHost;

    public IConnectionMultiplexer Redis { get; private set; } = null!;

    public Guid SharedCustomerId { get; private set; }

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public Task<TranzrMovesDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            new TranzrMovesDbContext(PayLaterE2eDatabaseBootstrap.CreateOptions(PostgresConnectionString)));
    }

    public IServiceScope CreateWorkerScope()
    {
        ArgumentNullException.ThrowIfNull(_workerHost);
        return _workerHost.Services.CreateScope();
    }

    public async Task<string> EnsureStripePaymentMethodAsync(CancellationToken cancellationToken = default)
    {
        using var scope = CreateWorkerScope();
        var stripeClient = scope.ServiceProvider.GetRequiredService<StripeClient>();
        return await PayLaterStripeTestHelper.EnsureCustomerWithPaymentMethodAsync(stripeClient, cancellationToken);
    }

    public async Task ExpirePayLaterKeyAsync(Guid quoteId)
    {
        var db = Redis.GetDatabase();
        var key = PayLaterChargeKeys.ForQuote(quoteId);
        var payload = $"{{\"quoteId\":\"{quoteId:N}\"}}";
        await db.StringSetAsync(key, payload, TimeSpan.FromSeconds(2));
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _rabbit.StartAsync(),
            _redis.StartAsync());

        await PayLaterE2eDatabaseBootstrap.ApplyAsync(PostgresConnectionString);
        SharedCustomerId = await SeedSharedCustomerAsync();

        Redis = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());

        _workerHost = CreateWorkerHost();
        await _workerHost.StartAsync();

        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    public async ValueTask DisposeAsync()
    {
        if (_workerHost is not null)
        {
            await _workerHost.StopAsync();
            _workerHost.Dispose();
        }

        if (Redis is not null)
        {
            await Redis.DisposeAsync();
        }

        await Task.WhenAll(
            _redis.DisposeAsync().AsTask(),
            _rabbit.DisposeAsync().AsTask(),
            _postgres.DisposeAsync().AsTask());
    }

    private async Task<Guid> SeedSharedCustomerAsync()
    {
        await using var db = new TranzrMovesDbContext(
            PayLaterE2eDatabaseBootstrap.CreateOptions(PostgresConnectionString));

        var customerId = Guid.NewGuid();
        var now = SystemClock.Instance.GetCurrentInstant();
        db.Set<UserV2>().Add(new UserV2
        {
            Id = customerId,
            Email = PayLaterStripeTestHelper.E2eCustomerEmail,
            FirstName = "Pay",
            LastName = "Later",
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = "e2e-fixture",
            ModifiedBy = "e2e-fixture"
        });
        await db.SaveChangesAsync();
        return customerId;
    }

    private IHost CreateWorkerHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Configuration.AddConfiguration(BuildConfiguration());

        TranzrMovesWorkerHost.Configure(builder, WorkerRole.All);

        builder.Services.RemoveAll<IEmailService>();
        builder.Services.AddSingleton<IEmailService, NoOpEmailService>();

        builder.Services.RemoveAll<ICollectQuoteV2BalanceChargePublisher>();
        builder.Services.AddScoped<ICollectQuoteV2BalanceChargePublisher, InProcessCollectQuoteV2BalanceChargePublisher>();

        return builder.Build();
    }

    /// <summary>
    /// Publishes a balance charge through the same publisher the Redis listener uses.
    /// </summary>
    public async Task PublishBalanceChargeAsync(CollectQuoteV2BalanceCharge message,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateWorkerScope();
        var publisher = scope.ServiceProvider.GetRequiredService<ICollectQuoteV2BalanceChargePublisher>();
        await publisher.PublishAsync(message, cancellationToken);
    }

    private IConfiguration BuildConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(PayLaterEndToEndFixture).Assembly, optional: true)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TranzrMovesDatabaseConnection"] = PostgresConnectionString,
                ["ConnectionStrings:redis"] = _redis.GetConnectionString(),
                ["ConnectionStrings:rabbitmq"] = _rabbit.GetConnectionString(),
                ["Worker:Role"] = nameof(WorkerRole.All),
                ["PayLater:UseDurableMessaging"] = "false",
                ["PayLater:RecoveryIntervalMinutes"] = "1440",
                ["COMMUNICATION_SERVICES_CONNECTION_STRING"] =
                    "endpoint=https://test.communication.azure.com/;accesskey=dGVzdA==",
                ["TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2"] = "whsec_test"
            })
            .Build();

        var stripeApiKey = PayLaterStripeTestHelper.RequireApiKey(configuration);

        return new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["STRIPE_API_KEY"] = stripeApiKey
            })
            .Build();
    }
}
