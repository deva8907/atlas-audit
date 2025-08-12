using Atlas.Audit.Models;

namespace Atlas.Audit.Interfaces;

public interface IAuditDataStrategy
{
    AuditData ConstructAuditData(object oldEntity, object newEntity, AuditOperation operation, string userId);
    Type EntityType { get; }
}

public interface IAuditable
{
    // Marker interface for auditable entities
}
