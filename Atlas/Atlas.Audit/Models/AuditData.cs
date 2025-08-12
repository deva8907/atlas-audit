namespace Atlas.Audit.Models;

public class AuditData
{
    public string TableName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditOperation Operation { get; set; }
    public Dictionary<string, object>? OldValues { get; set; }
    public Dictionary<string, object>? NewValues { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public enum AuditOperation
{
    Insert,
    Update,
    Delete
}
