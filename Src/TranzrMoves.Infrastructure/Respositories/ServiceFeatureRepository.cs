using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Entities;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Respositories;

public class ServiceFeatureRepository(TranzrMovesDbContext dbContext, ILogger<ServiceFeatureRepository> logger) : IServiceFeatureRepository
{
    public async Task<ErrorOr<ServiceFeature>> AddServiceFeatureAsync(ServiceFeature serviceFeature, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Add(serviceFeature);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully added service feature {Id} for {ServiceLevel} service level", 
                serviceFeature.Id, serviceFeature.ServiceLevel);
            
            return serviceFeature;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add service feature");
            return Error.Failure("ServiceFeature.AddError", "Failed to add service feature to database");
        }
    }

    public async Task<ServiceFeature?> GetServiceFeatureAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Set<ServiceFeature>()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<ServiceFeature>> GetServiceFeaturesAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<ServiceFeature>().AsQueryable();
        
        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }
        
        return await query
            .OrderBy(s => s.ServiceLevel)
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ErrorOr<ServiceFeature>> UpdateServiceFeatureAsync(ServiceFeature serviceFeature, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Update(serviceFeature);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully updated service feature {Id}", serviceFeature.Id);
            
            return serviceFeature;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating service feature {Id}", serviceFeature.Id);
            return Error.Conflict("ServiceFeature.ConcurrencyConflict", "The service feature was modified by another user");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update service feature {Id}", serviceFeature.Id);
            return Error.Failure("ServiceFeature.UpdateError", "Failed to update service feature in database");
        }
    }

    public async Task DeleteServiceFeatureAsync(ServiceFeature serviceFeature, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Remove(serviceFeature);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully deleted service feature {Id}", serviceFeature.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete service feature {Id}", serviceFeature.Id);
            throw;
        }
    }
}
