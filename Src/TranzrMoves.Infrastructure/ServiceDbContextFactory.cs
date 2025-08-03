using AdminClientHandlerService.Domain.Constants;
using AdminClientHandlerService.Infrastructure.Data;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdminClientHandlerService.Infrastructure;

public class ServiceDbContextFactory : IDesignTimeDbContextFactory<AdminClientHandlerDbContext>
{
    public AdminClientHandlerDbContext CreateDbContext(string[] args)
    {
        var localDockerDbConnectionString =
            "Server=localhost;Port=5433;Database=admin-client-service;User Id=postgres;Password=postgres;";
            // "Server=admin-client-db;Port=5433;Database=admin-client-handler-service;User Id=postgres;Password=postgres;";
        
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? localDockerDbConnectionString;
        Guard.Against.NullOrEmpty(connectionString, $"Connection string {Db.CONNECTION_STRING_NAME} is missing. Please set the environment variable ConnectionStrings::{Db.CONNECTION_STRING_NAME}.");
        
        var optionsBuilder = new DbContextOptionsBuilder<AdminClientHandlerDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            x => x.MigrationsHistoryTable("_MigrationHistory", Db.SCHEMA));
        return new AdminClientHandlerDbContext(optionsBuilder.Options);
    }
}