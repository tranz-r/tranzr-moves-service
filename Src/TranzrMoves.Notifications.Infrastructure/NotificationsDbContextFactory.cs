using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TranzrMoves.Notifications.Infrastructure.Constants;

namespace TranzrMoves.Notifications.Infrastructure;

public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var localDockerDbConnectionString =
            "Server=localhost;Port=5432;Database=tranzr;User Id=tranzr;Password=tranzr;";

        var connectionString =
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? localDockerDbConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<NotificationsDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__MigrationHistory", NotificationsDb.Schema);
            npgsql.UseNodaTime();
        });

        return new NotificationsDbContext(optionsBuilder.Options);
    }
}
