using Microsoft.EntityFrameworkCore;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Infrastructure;

namespace TranzrMoves.IntegrationTests.Helpers;

internal static class PayLaterE2eDatabaseBootstrap
{
    public static async Task ApplyAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var context = new TranzrMovesDbContext(CreateOptions(connectionString));
        await context.Database.MigrateAsync(cancellationToken);
    }

    public static DbContextOptions<TranzrMovesDbContext> CreateOptions(string connectionString) =>
        new DbContextOptionsBuilder<TranzrMovesDbContext>()
            .UseNpgsql(connectionString, o =>
            {
                o.MigrationsHistoryTable("__MigrationHistory", Db.SCHEMA);
                o.UseNodaTime();
            })
            .Options;
}
