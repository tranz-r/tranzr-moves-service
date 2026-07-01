using System.Reflection;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure;

public class TranzrMovesDbContext(
    DbContextOptions<TranzrMovesDbContext> dbContextOptions,
    ITenantProvider? tenantProvider = null)
    : DbContext(dbContextOptions), IDataProtectionKeyContext
{
    private static readonly MethodInfo ApplyTenantFilterMethod = typeof(TranzrMovesDbContext)
        .GetMethod(nameof(ApplyTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    // DbSets
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    public DbSet<BusinessUserRoleChange> BusinessUserRoleChanges => Set<BusinessUserRoleChange>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    // Read by the tenant query filter at query-execution time. When no tenant is
    // resolved (system/anonymous/design-time/tests) filtering is disabled.
    private bool TenantFilterEnabled => tenantProvider?.BusinessAccountId is not null;

    private Guid CurrentTenantId => tenantProvider?.BusinessAccountId ?? Guid.Empty;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.UseExceptionProcessor();

        // BusinessUser carries a tenant filter while its required principals
        // (BusinessAccount, UserV2) do not; this interaction warning is expected.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                ApplyTenantFilterMethod
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, [modelBuilder]);
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => !TenantFilterEnabled || e.BusinessAccountId == CurrentTenantId);
    }
}
