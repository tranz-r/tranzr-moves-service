using System.Reflection;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure;

public class TranzrMovesDbContext(DbContextOptions<TranzrMovesDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
    public TranzrMovesDbContext() : this(new  DbContextOptionsBuilder<TranzrMovesDbContext>().Options)
    {}

    // DbSets
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    
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