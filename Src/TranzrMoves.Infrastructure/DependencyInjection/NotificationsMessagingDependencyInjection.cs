using Microsoft.Extensions.Configuration;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Notifications.Contracts;
using TranzrMoves.Notifications.Infrastructure.DependencyInjection;
using TranzrMoves.Notifications.Infrastructure.Options;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace TranzrMoves.Infrastructure.DependencyInjection;

public static class NotificationsMessagingDependencyInjection
{
    public static WolverineOptions ConfigureNotificationsPublisher(
        this WolverineOptions options,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(Db.CONNECTION_STRING_NAME)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{Db.CONNECTION_STRING_NAME}' is not configured.");

        var notificationsOptions = configuration.GetSection(NotificationsOptions.SectionName)
            .Get<NotificationsOptions>() ?? new NotificationsOptions();

        options.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

        if (notificationsOptions.UseDurableMessaging)
        {
            options.PersistMessagesWithPostgresql(connectionString);
            options.Durability.MessageStorageSchemaName = Db.SCHEMA;
            options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        }

        options.PublishMessage<SendNotification>()
            .ToRabbitQueue(NotificationsWolverineExtensions.NotificationsQueueName);

        options.UseEntityFrameworkCoreTransactions();

        return options;
    }
}
