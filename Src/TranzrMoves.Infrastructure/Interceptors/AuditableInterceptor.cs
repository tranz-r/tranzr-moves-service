using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Interceptors;

public class AuditableInterceptor() : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext context)
    {
        DateTime utcNow = DateTime.UtcNow;
        var entities = context.ChangeTracker.Entries<IAuditable>().ToList();

        foreach (EntityEntry<IAuditable> entry in entities)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetCurrentPropertyDateTimeValue(entry, 
                        nameof(IAuditable.CreatedAt), utcNow);
                    SetCurrentPropertyDateTimeValue(entry, 
                        nameof(IAuditable.ModifiedAt), utcNow);
                    SetCurrentPropertyValue(entry, 
                        nameof(IAuditable.CreatedBy), "System");
                    break;
                case EntityState.Modified:
                    SetCurrentPropertyDateTimeValue(entry, 
                        nameof(IAuditable.ModifiedAt), utcNow);
                    SetCurrentPropertyValue(entry, 
                        nameof(IAuditable.ModifiedBy), "System");
                    break;
            }
        }

        static void SetCurrentPropertyDateTimeValue(
            EntityEntry entry,
            string propertyName,
            DateTime utcNow) =>
            entry.Property(propertyName).CurrentValue = utcNow;

        static void SetCurrentPropertyValue(
            EntityEntry entry,
            string propertyName,
            string value) =>
            entry.Property(propertyName).CurrentValue = value;
    }
}