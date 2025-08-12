using Atlas.Audit.Models;
using Atlas.Audit.Services;
using Atlas.Visit.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.Visit.Strategies;

public class VisitAuditDataStrategy : BaseAuditDataStrategy<Models.Visit>
{
    public VisitAuditDataStrategy(ILogger<VisitAuditDataStrategy> logger) 
        : base(logger) { }

    protected override string GetTableName() => "Visits";

    protected override string GetEntityId(Models.Visit entity) => entity?.Id.ToString() ?? string.Empty;

    protected override Dictionary<string, object> ExtractValues(Models.Visit entity)
    {
        if (entity == null) return new Dictionary<string, object>();

        return new Dictionary<string, object>
        {
            ["Id"] = entity.Id,
            ["PatientId"] = entity.PatientId,
            ["VisitDate"] = entity.VisitDate,
            ["VisitType"] = entity.VisitType,
            ["ChiefComplaint"] = entity.ChiefComplaint,
            ["Diagnosis"] = entity.Diagnosis,
            ["Treatment"] = entity.Treatment,
            ["Notes"] = entity.Notes,
            ["Status"] = entity.Status.ToString(),
            ["CreatedAt"] = entity.CreatedAt,
            ["UpdatedAt"] = entity.UpdatedAt
        };
    }
}
