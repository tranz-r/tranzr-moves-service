using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TranzrMoves.Infrastructure;

public class ServiceDbContextFactory : IDesignTimeDbContextFactory<TranzrMovesDbContext>
{
    public TranzrMovesDbContext CreateDbContext(string[] args)
    {
        var localDockerDbConnectionString =
            "Server=localhost;Port=5433;Database=admin-client-service;User Id=postgres;Password=postgres;";
            // "Server=admin-client-db;Port=5433;Database=admin-client-handler-service;User Id=postgres;Password=postgres;";
        
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? localDockerDbConnectionString;
        
        var optionsBuilder = new DbContextOptionsBuilder<TranzrMovesDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new TranzrMovesDbContext(optionsBuilder.Options);
    }
}