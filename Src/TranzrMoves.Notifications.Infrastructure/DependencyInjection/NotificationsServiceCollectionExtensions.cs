using Azure.Communication.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using TranzrMoves.Notifications.Infrastructure.Constants;
using TranzrMoves.Notifications.Infrastructure.EmailTemplates;
using TranzrMoves.Notifications.Infrastructure.Interfaces;
using TranzrMoves.Notifications.Infrastructure.Options;
using TranzrMoves.Notifications.Infrastructure.Services;

namespace TranzrMoves.Notifications.Infrastructure.DependencyInjection;

public static class NotificationsServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<NotificationsOptions>(configuration.GetSection(NotificationsOptions.SectionName));
        services.AddSingleton<IClock>(_ => SystemClock.Instance);

        var connectionString = configuration.GetConnectionString(NotificationsDb.ConnectionStringName)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{NotificationsDb.ConnectionStringName}' is not configured.");

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__MigrationHistory", NotificationsDb.Schema);
                npgsql.UseNodaTime();
            }));

        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IEmailSender>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<NotificationsOptions>>().Value;
            if (string.Equals(options.EmailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                return ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp);
            }

            if (string.Equals(options.EmailProvider, "Acs", StringComparison.OrdinalIgnoreCase))
            {
                var acs = configuration["COMMUNICATION_SERVICES_CONNECTION_STRING"]
                          ?? throw new InvalidOperationException(
                              "COMMUNICATION_SERVICES_CONNECTION_STRING is not configured for Notifications:EmailProvider=Acs.");

                var emailClient = new EmailClient(acs);
                return ActivatorUtilities.CreateInstance<AzureEmailSender>(sp, emailClient);
            }

            throw new InvalidOperationException(
                $"Unsupported Notifications:EmailProvider '{options.EmailProvider}'. Valid values: Acs, Smtp.");
        });

        return services;
    }
}
