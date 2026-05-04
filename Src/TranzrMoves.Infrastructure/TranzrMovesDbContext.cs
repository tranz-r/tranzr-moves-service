using System.Reflection;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure;

public class TranzrMovesDbContext(DbContextOptions<TranzrMovesDbContext> dbContextOptions)
    : DbContext(dbContextOptions), IDataProtectionKeyContext
{
    public TranzrMovesDbContext() : this(new DbContextOptionsBuilder<TranzrMovesDbContext>().Options)
    { }

    // DbSets
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.UseExceptionProcessor();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
