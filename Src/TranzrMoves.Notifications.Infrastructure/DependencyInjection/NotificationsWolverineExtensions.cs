using Microsoft.Extensions.Configuration;
using TranzrMoves.Notifications.Contracts;
using TranzrMoves.Notifications.Infrastructure.Constants;
using TranzrMoves.Notifications.Infrastructure.Options;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace TranzrMoves.Notifications.Infrastructure.DependencyInjection;

public static class NotificationsWolverineExtensions
{
    public const string NotificationsQueueName = "notifications-send";

    public static WolverineOptions ConfigureNotificationsMessaging(
        this WolverineOptions options,
        IConfiguration configuration,
        bool includeConsumer)
    {
        var connectionString = configuration.GetConnectionString(NotificationsDb.ConnectionStringName)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{NotificationsDb.ConnectionStringName}' is not configured.");

        var notificationsOptions = configuration.GetSection(NotificationsOptions.SectionName)
            .Get<NotificationsOptions>() ?? new NotificationsOptions();

        options.Discovery.IncludeAssembly(typeof(SendNotification).Assembly);
        options.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

        if (notificationsOptions.UseDurableMessaging)
        {
            options.PersistMessagesWithPostgresql(connectionString);
            options.Durability.MessageStorageSchemaName = NotificationsDb.Schema;
            if (includeConsumer)
            {
                options.Policies.UseDurableInboxOnAllListeners();
            }
        }

        options.PublishMessage<SendNotification>().ToRabbitQueue(NotificationsQueueName);

        if (includeConsumer)
        {
            options.UseEntityFrameworkCoreTransactions();

            var sendListener = options.ListenToRabbitQueue(NotificationsQueueName);
            if (notificationsOptions.UseDurableMessaging)
            {
                sendListener.UseDurableInbox();
            }
        }

        return options;
    }

    public static WolverineOptions IncludeNotificationsHandlers(
        this WolverineOptions options,
        System.Reflection.Assembly handlersAssembly)
    {
        options.Discovery.IncludeAssembly(handlersAssembly);
        return options;
    }
}
