using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Helper;

/// <summary>
/// Runs at API startup (after <c>WebApplication.Build</c>) so removal pricing exists once the DB schema is current.
/// Seeds rate cards, service features, and additional prices (assembly / dismantle). Migrations are applied separately; this only inserts missing rows (idempotent).
/// </summary>
public static class RemovalPricingSeeder
{
    /// <summary>Matches seeded API payloads: <c>2025-08-29T00:00:00Z</c>.</summary>
    private static readonly Instant AdditionalPricesSeedEffectiveFrom =
        new LocalDate(2025, 8, 29).AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();

    public static async Task SeedAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranzrMovesDbContext>();
        var timeService = scope.ServiceProvider.GetRequiredService<ITimeService>();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("RemovalPricingSeeder");

        // If DB can't be reached, bail quietly
        if (!await db.Database.CanConnectAsync(ct))
        {
            logger?.LogWarning("Cannot connect to database; skipping pricing seed.");
            return;
        }

        // Ensure target tables exist (schema + names come from the EF model, not hard-coded public.* guesses)
        if (!await TableExistsAsync(db, typeof(RateCard), ct) ||
            !await TableExistsAsync(db, typeof(ServiceFeature), ct))
        {
            logger?.LogWarning("Tables not found (RateCards/ServiceFeatures); skipping pricing seed.");
            return;
        }

        var additionalPricesTableOk = await TableExistsAsync(db, typeof(AdditionalPrice), ct);
        if (!additionalPricesTableOk)
            logger?.LogWarning("Table not found (AdditionalPrices); skipping additional price seed.");

        var utc = DateTimeZone.Utc;
        var now = timeService.Now();
        var today = timeService.TodayInUtc().AtStartOfDayInZone(utc).ToInstant();

        // ---- helpers ----
        // Auditable timestamps / CreatedBy are applied by AuditableInterceptor on SaveChanges.
        static RateCard RC(int movers, ServiceLevel level, int hours, decimal block, decimal after, Instant from) =>
            new()
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
                IsActive = true
            };

        static ServiceFeature SF(ServiceLevel level, int order, string text, Instant from) => new()
        {
            Id = Guid.NewGuid(),
            ServiceLevel = level,
            DisplayOrder = order,
            Text = text,
            EffectiveFrom = from,
            EffectiveTo = null,
            IsActive = true
        };

        static AdditionalPrice AP(AdditionalPriceType type, string description, decimal price, string currency, Instant from) =>
            new()
            {
                Id = Guid.NewGuid(),
                Type = type,
                Description = description,
                Price = price,
                CurrencyCode = currency,
                EffectiveFrom = from,
                EffectiveTo = null,
                IsActive = true
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
                db.Add(RC(movers, level, hours, block, after, today));
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
                db.Add(SF(level, order, text, today));
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

        if (additionalPricesTableOk)
        {
            async Task EnsureAdditionalAsync(AdditionalPriceType type, string description, decimal price, string currency)
            {
                var exists = await db.Set<AdditionalPrice>().AnyAsync(p =>
                    p.Type == type &&
                    p.IsActive &&
                    p.EffectiveFrom <= now &&
                    (p.EffectiveTo == null || p.EffectiveTo > now), ct);

                if (!exists)
                    db.Add(AP(type, description, price, currency, AdditionalPricesSeedEffectiveFrom));
            }

            await EnsureAdditionalAsync(
                AdditionalPriceType.Assembly,
                "Professional Assembly Service",
                25.00m,
                "GBP");
            await EnsureAdditionalAsync(
                AdditionalPriceType.Dismantle,
                "Professional Dismantling Service",
                18.00m,
                "GBP");
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
            logger?.LogInformation("Removal pricing seed inserted new rate cards, features, or additional prices.");
        }
        else
            logger?.LogInformation("Removal pricing seed skipped inserts (current data already present).");
    }

    private static async Task<bool> TableExistsAsync(DbContext db, Type entityClrType, CancellationToken ct)
    {
        var entityType = db.Model.FindEntityType(entityClrType);
        if (entityType == null) return false;

        var table = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "public";
        if (string.IsNullOrEmpty(table)) return false;

        // Do not dispose: this is the DbContext's shared connection; disposing breaks subsequent use.
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            """
            select exists (
              select 1
              from pg_catalog.pg_class c
              join pg_catalog.pg_namespace n on n.oid = c.relnamespace
              where n.nspname = @schema
                and c.relname = @table
                and c.relkind = 'r');
            """,
            conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is bool b && b;
    }
}