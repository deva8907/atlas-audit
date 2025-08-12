using Atlas.Audit.Interfaces;
using Atlas.Audit.Models;
using Atlas.Audit.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Atlas.Audit.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly AuditDataStrategyFactory _strategyFactory;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditInterceptor> _logger;

    public AuditInterceptor(
        AuditDataStrategyFactory strategyFactory,
        IAuditService auditService,
        ILogger<AuditInterceptor> logger)
    {
        _strategyFactory = strategyFactory;
        _auditService = auditService;
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEntries = CaptureAuditData(eventData.Context);
            // Process audit entries immediately for simplicity
            _ = Task.Run(async () =>
            {
                foreach (var auditData in auditEntries)
                {
                    try
                    {
                        await _auditService.SaveAuditDataAsync(auditData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save audit entry for {TableName}", auditData.TableName);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture audit data during SaveChanges");
            // Don't rethrow - audit failures shouldn't break the main operation
        }
        
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        return base.SavedChanges(eventData, result);
    }

    private List<AuditData> CaptureAuditData(DbContext context)
    {
        var auditDataList = new List<AuditData>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable && 
                (entry.State == EntityState.Added || 
                 entry.State == EntityState.Modified || 
                 entry.State == EntityState.Deleted))
            {
                var entityType = entry.Entity.GetType();
                
                if (!_strategyFactory.HasStrategy(entityType))
                {
                    continue; // Skip entities without audit strategies
                }

                try
                {
                    var strategy = _strategyFactory.GetStrategy(entityType);

                    var operation = entry.State switch
                    {
                        EntityState.Added => AuditOperation.Insert,
                        EntityState.Modified => AuditOperation.Update,
                        EntityState.Deleted => AuditOperation.Delete,
                        _ => throw new InvalidOperationException($"Unsupported entity state: {entry.State}")
                    };

                    var oldEntity = operation == AuditOperation.Insert ? null : entry.Entity;
                    var newEntity = operation == AuditOperation.Delete ? null : entry.Entity;

                    var auditData = strategy.ConstructAuditData(oldEntity, newEntity, operation, "system");
                    auditDataList.Add(auditData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to capture audit data for entity type {EntityType}", entityType.Name);
                    // Continue processing other entities
                }
            }
        }

        return auditDataList;
    }
}
