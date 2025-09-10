using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Helper;

public static class RemovalPricingSeeder
{
    public static async Task SeedAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranzrMovesDbContext>();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("RemovalPricingSeeder");

        // If DB can't be reached, bail quietly
        if (!await db.Database.CanConnectAsync(ct))
        {
            logger?.LogWarning("Cannot connect to database; skipping pricing seed.");
            return;
        }

        // Ensure target tables exist (no migrations)
        if (!await TableExistsAsync(db, "rate_cards", ct) || !await TableExistsAsync(db, "service_features", ct))
        {
            logger?.LogWarning("Tables not found (rate_cards/service_features); skipping pricing seed.");
            return;
        }

        var today = DateTimeOffset.UtcNow.Date;
        var now   = DateTimeOffset.UtcNow;

        // ---- helpers ----
        static RateCard RC(int movers, ServiceLevel level, int hours, decimal block, decimal after,
            DateTimeOffset from, DateTimeOffset now) => new()
        {
            Id = Guid.NewGuid(),
            Movers = movers,
            ServiceLevel = level,
            BaseBlockHours = hours,
            BaseBlockPrice = block,
            HourlyRateAfter = after,
            CurrencyCode = "GBP",
            EffectiveFrom = from,
            EffectiveTo = null,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "Seed",
            ModifiedAt = now,
            ModifiedBy = "Seed"
        };

        static ServiceFeature SF(ServiceLevel level, int order, string text,
            DateTimeOffset from, DateTimeOffset now) => new()
        {
            Id = Guid.NewGuid(),
            ServiceLevel = level,
            DisplayOrder = order,
            Text = text,
            EffectiveFrom = from,
            EffectiveTo = null,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "Seed",
            ModifiedAt = now,
            ModifiedBy = "Seed"
        };

        // ---- idempotent inserts ----
        async Task EnsureRateAsync(int movers, ServiceLevel level, int hours, decimal block, decimal after)
        {
            var exists = await db.Set<RateCard>().AnyAsync(r =>
                r.Movers == movers &&
                r.ServiceLevel == level &&
                r.IsActive &&
                r.EffectiveFrom <= now &&
                (r.EffectiveTo == null || r.EffectiveTo > now), ct);

            if (!exists)
                db.Add(RC(movers, level, hours, block, after, today, now));
        }

        await EnsureRateAsync(1, ServiceLevel.Standard, 3, 250, 65);
        await EnsureRateAsync(1, ServiceLevel.Premium,  4, 350, 75);
        await EnsureRateAsync(2, ServiceLevel.Standard, 3, 300, 75);
        await EnsureRateAsync(2, ServiceLevel.Premium,  4, 450, 85);
        await EnsureRateAsync(3, ServiceLevel.Standard, 3, 350, 85);
        await EnsureRateAsync(3, ServiceLevel.Premium,  4, 500, 95);

        async Task EnsureFeatureAsync(ServiceLevel level, int order, string text)
        {
            var exists = await db.Set<ServiceFeature>().AnyAsync(f =>
                f.ServiceLevel == level &&
                f.DisplayOrder == order &&
                f.Text == text &&
                f.IsActive &&
                f.EffectiveFrom <= now &&
                (f.EffectiveTo == null || f.EffectiveTo > now), ct);

            if (!exists)
                db.Add(SF(level, order, text, today, now));
        }

        // Standard features
        await EnsureFeatureAsync(ServiceLevel.Standard, 1, "Moving team to load and move");
        await EnsureFeatureAsync(ServiceLevel.Standard, 2, "Professional moving equipment");
        await EnsureFeatureAsync(ServiceLevel.Standard, 3, "Insurance coverage included");
        await EnsureFeatureAsync(ServiceLevel.Standard, 4, "Careful handling of items");
        await EnsureFeatureAsync(ServiceLevel.Standard, 5, "On-time service guarantee");

        // Premium features
        await EnsureFeatureAsync(ServiceLevel.Premium, 1, "Everything in Standard");
        await EnsureFeatureAsync(ServiceLevel.Premium, 2, "Priority scheduling");
        await EnsureFeatureAsync(ServiceLevel.Premium, 3, "Premium packaging materials");
        await EnsureFeatureAsync(ServiceLevel.Premium, 4, "Post-move support");
        await EnsureFeatureAsync(ServiceLevel.Premium, 5, "Satisfaction guarantee");
        await EnsureFeatureAsync(ServiceLevel.Premium, 6, "Dismantling and reassembly included");

        await db.SaveChangesAsync(ct);
        logger?.LogInformation("Removal pricing seed completed.");
    }

    private static async Task<bool> TableExistsAsync(DbContext db, string table, CancellationToken ct)
    {
        await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(ct);

        // checks current schema ('public' by default); adjust schema if you use another
        await using var cmd = new NpgsqlCommand("select to_regclass(@p1) is not null;", conn);
        cmd.Parameters.AddWithValue("p1", $"public.{table}");
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is bool b && b;
    }
}