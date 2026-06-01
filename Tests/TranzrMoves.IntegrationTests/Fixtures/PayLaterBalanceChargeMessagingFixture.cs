using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NSubstitute;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.DependencyInjection;
using Wolverine;

namespace TranzrMoves.IntegrationTests.Fixtures;

public sealed class PayLaterBalanceChargeMessagingFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase($"paylater_messaging_{Guid.NewGuid():N}")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private IHost? _host;

    public IQuoteV2LaterBalanceCollectionService CollectionService { get; private set; } = null!;

    public IQuoteV2DepositBalanceCollectionService DepositCollectionService { get; private set; } = null!;

    public IQuoteRepository QuoteRepository { get; private set; } = null!;

    public async Task PublishAsync(CollectQuoteV2BalanceCharge message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(_host);
        cancellationToken.ThrowIfCancellationRequested();

        await using var scope = _host.Services.CreateAsyncScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(message);
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());

        CollectionService = Substitute.For<IQuoteV2LaterBalanceCollectionService>();
        CollectionService.TryCollectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ErrorOr<Success>>(Result.Success));

        DepositCollectionService = Substitute.For<IQuoteV2DepositBalanceCollectionService>();
        DepositCollectionService.TryCollectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ErrorOr<Success>>(Result.Success));

        QuoteRepository = Substitute.For<IQuoteRepository>();
        QuoteRepository.GetQuoteByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new QuoteV2
            {
                Id = callInfo.Arg<Guid>(),
                PaymentStatus = PaymentStatus.PaymentSetup,
                QuoteReference = "messaging-test"
            });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TranzrMovesDatabaseConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:rabbitmq"] = _rabbit.GetConnectionString(),
                ["PayLater:UseDurableMessaging"] = "false"
            })
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddSingleton<IClock>(_ => SystemClock.Instance);
        builder.Services.AddSingleton<ITimeService, TimeService>();
        builder.Services.AddTranzrMovesDatabase(configuration);
        builder.Services.AddScoped(_ => CollectionService);
        builder.Services.AddScoped(_ => DepositCollectionService);
        builder.Services.AddScoped(_ => QuoteRepository);

        builder.UseWolverine(opts =>
            opts.ConfigurePayLaterMessaging(configuration, includeConsumer: true));

        _host = builder.Build();
        await _host.StartAsync();

        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        await Task.WhenAll(_rabbit.DisposeAsync().AsTask(), _postgres.DisposeAsync().AsTask());
    }
}
