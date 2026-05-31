using Microsoft.Extensions.Configuration;
using TranzrMoves.Application.Messaging;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Infrastructure.Services;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class WolverineDependencyInjection
{
    public const string BalanceChargeQueueName = "quote-v2-balance-charge";

    public static WolverineOptions ConfigurePayLaterMessaging(
        this WolverineOptions options,
        IConfiguration configuration,
        bool includeConsumer)
    {
        var connectionString = configuration.GetConnectionString(Db.CONNECTION_STRING_NAME)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{Db.CONNECTION_STRING_NAME}' is not configured.");

        var payLaterOptions = configuration.GetSection(PayLaterOptions.SectionName).Get<PayLaterOptions>()
                              ?? new PayLaterOptions();
        var useDurableMessaging = payLaterOptions.UseDurableMessaging;

        options.Discovery.IncludeAssembly(typeof(CollectQuoteV2BalanceCharge).Assembly);

        options.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

        if (useDurableMessaging)
        {
            options.PersistMessagesWithPostgresql(connectionString);
            options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        }

        options.PublishMessage<CollectQuoteV2BalanceCharge>()
            .ToRabbitQueue(BalanceChargeQueueName);

        if (includeConsumer)
        {
            // Required when handlers use EF Core alongside PersistMessagesWithPostgresql / durable inbox.
            // Without this, DbContext access in message handlers fails with a misleading
            // "No database provider has been configured" error (missing DatabaseSettings).
            options.UseEntityFrameworkCoreTransactions();

            var listener = options.ListenToRabbitQueue(BalanceChargeQueueName);
            if (useDurableMessaging)
            {
                listener.UseDurableInbox();
            }
        }

        return options;
    }
}
