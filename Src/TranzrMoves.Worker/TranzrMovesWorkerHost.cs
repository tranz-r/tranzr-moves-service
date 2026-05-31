using Stripe;
using TranzrMoves.Infrastructure.DependencyInjection;

namespace TranzrMoves.Worker;

public static class TranzrMovesWorkerHost
{
    public static void Configure(IHostApplicationBuilder builder, WorkerRole role)
    {
        var includeRedis = role is WorkerRole.Scheduler or WorkerRole.All;
        var includeBalanceCollection = role is WorkerRole.Processor or WorkerRole.All;

        builder.Services.AddTranzrMovesDatabase(builder.Configuration);
        builder.Services.AddPayLaterWorkerServices(
            builder.Configuration,
            includeRedis,
            includeBalanceCollection);

        if (WorkerHostConfiguration.RequiresStripe(role))
        {
            builder.Services.AddSingleton(_ => new StripeClient(builder.Configuration["STRIPE_API_KEY"]));
        }

        builder.AddPayLaterWorkerRole(role);
    }
}
