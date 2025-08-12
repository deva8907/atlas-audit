using Atlas.Audit.Interfaces;

namespace Atlas.Visit.Models;

public class Visit : IAuditable
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public DateTime VisitDate { get; set; }
    public string VisitType { get; set; } = string.Empty;
    public string ChiefComplaint { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public VisitStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum VisitStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}
