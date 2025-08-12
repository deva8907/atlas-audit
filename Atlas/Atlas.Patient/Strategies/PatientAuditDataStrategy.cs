using Atlas.Audit.Models;
using Atlas.Audit.Services;
using Atlas.Patient.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.Patient.Strategies;

public class PatientAuditDataStrategy : BaseAuditDataStrategy<Models.Patient>
{
    public PatientAuditDataStrategy(ILogger<PatientAuditDataStrategy> logger) 
        : base(logger) { }

    protected override string GetTableName() => "Patients";

    protected override string GetEntityId(Models.Patient entity) => entity?.Id.ToString() ?? string.Empty;

    protected override Dictionary<string, object> ExtractValues(Models.Patient entity)
    {
        if (entity == null) return new Dictionary<string, object>();

        return new Dictionary<string, object>
        {
            ["Id"] = entity.Id,
            ["FirstName"] = entity.FirstName,
            ["LastName"] = entity.LastName,
            ["DateOfBirth"] = entity.DateOfBirth,
            ["MedicalRecordNumber"] = entity.MedicalRecordNumber,
            ["PhoneNumber"] = entity.PhoneNumber,
            ["Email"] = entity.Email,
            ["Address"] = entity.Address,
            ["IsActive"] = entity.IsActive,
            ["CreatedAt"] = entity.CreatedAt,
            ["UpdatedAt"] = entity.UpdatedAt
        };
    }
}
