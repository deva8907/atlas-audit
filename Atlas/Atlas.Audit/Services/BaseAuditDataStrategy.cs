using Atlas.Audit.Interfaces;
using Atlas.Audit.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.Audit.Services;

public abstract class BaseAuditDataStrategy<T> : IAuditDataStrategy where T : class
{
    protected readonly ILogger<BaseAuditDataStrategy<T>> _logger;

    protected BaseAuditDataStrategy(ILogger<BaseAuditDataStrategy<T>> logger)
    {
        _logger = logger;
    }

    public Type EntityType => typeof(T);

    public AuditData ConstructAuditData(object oldEntity, object newEntity, AuditOperation operation, string userId)
    {
        var typedOldEntity = oldEntity as T;
        var typedNewEntity = newEntity as T;

        var auditData = new AuditData
        {
            TableName = GetTableName(),
            EntityId = GetEntityId(typedNewEntity ?? typedOldEntity),
            Operation = operation,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };

        switch (operation)
        {
            case AuditOperation.Insert:
                auditData.OldValues = null;
                auditData.NewValues = ExtractValues(typedNewEntity);
                break;

            case AuditOperation.Update:
                auditData.OldValues = ExtractValues(typedOldEntity);
                auditData.NewValues = ExtractValues(typedNewEntity);
                break;

            case AuditOperation.Delete:
                auditData.OldValues = ExtractValues(typedOldEntity);
                auditData.NewValues = null;
                break;
        }

        return auditData;
    }

    protected abstract string GetTableName();
    protected abstract string GetEntityId(T entity);
    protected abstract Dictionary<string, object> ExtractValues(T entity);
}
