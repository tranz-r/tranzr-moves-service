using TranzrMoves.Infrastructure.DependencyInjection;
using TranzrMoves.Worker.HostedServices;
using Wolverine;

namespace TranzrMoves.Worker;

public static class WorkerHostConfiguration
{
    public const string RoleConfigurationKey = "Worker:Role";

    public static string GetObservabilityServiceName(WorkerRole role) =>
        role switch
        {
            WorkerRole.Scheduler => "tranzr-moves-worker-scheduler",
            WorkerRole.Processor => "tranzr-moves-worker-processor",
            WorkerRole.All => "tranzr-moves-worker-all",
            _ => "tranzr-moves-worker",
        };

    public static WorkerRole GetWorkerRole(IConfiguration configuration, IHostEnvironment environment)
    {
        var roleValue = configuration[RoleConfigurationKey];
        if (string.IsNullOrWhiteSpace(roleValue))
        {
            throw new InvalidOperationException(
                $"Configuration '{RoleConfigurationKey}' is required. Set to 'Scheduler' or 'Processor' (or 'All' in Development only).");
        }

        if (!Enum.TryParse<WorkerRole>(roleValue, ignoreCase: true, out var role) || !Enum.IsDefined(role))
        {
            throw new InvalidOperationException(
                $"Invalid '{RoleConfigurationKey}' value '{roleValue}'. Valid values: Scheduler, Processor, All (Development only).");
        }

        if (role == WorkerRole.All && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Worker:Role=All is only permitted in the Development environment.");
        }

        return role;
    }

    public static bool RequiresStripe(WorkerRole role) =>
        role is WorkerRole.Processor or WorkerRole.All;

    public static void RegisterPayLaterHostedServices(IServiceCollection services, WorkerRole role)
    {
        if (role is WorkerRole.Scheduler or WorkerRole.All)
        {
            services.AddHostedService<BalanceChargeExpiryListener>();
            services.AddHostedService<BalanceChargeRecoveryWorker>();
        }
    }

    public static void AddPayLaterWorkerRole(this IHostApplicationBuilder builder, WorkerRole role)
    {
        RegisterPayLaterHostedServices(builder.Services, role);

        builder.UseWolverine(opts =>
        {
            opts.ServiceName = GetObservabilityServiceName(role);
            opts.ConfigurePayLaterMessaging(
                builder.Configuration,
                includeConsumer: role is WorkerRole.Processor or WorkerRole.All);

            if (role is WorkerRole.Processor or WorkerRole.All)
            {
                opts.ConfigureNotificationsPublisher(builder.Configuration);
            }
        });
    }
}
