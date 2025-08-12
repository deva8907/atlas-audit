using Atlas.Visit.Data;
using Atlas.Visit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Atlas.Visit.Services;

public interface IVisitService
{
    Task<IEnumerable<Models.Visit>> GetAllVisitsAsync();
    Task<Models.Visit?> GetVisitByIdAsync(int id);
    Task<IEnumerable<Models.Visit>> GetVisitsByPatientIdAsync(int patientId);
    Task<Models.Visit> CreateVisitAsync(Models.Visit visit);
    Task<Models.Visit> UpdateVisitAsync(Models.Visit visit);
    Task DeleteVisitAsync(int id);
}

public class VisitService : IVisitService
{
    private readonly VisitDbContext _context;
    private readonly ILogger<VisitService> _logger;

    public VisitService(VisitDbContext context, ILogger<VisitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Models.Visit>> GetAllVisitsAsync()
    {
        return await _context.Visits.ToListAsync();
    }

    public async Task<Models.Visit?> GetVisitByIdAsync(int id)
    {
        return await _context.Visits.FindAsync(id);
    }

    public async Task<IEnumerable<Models.Visit>> GetVisitsByPatientIdAsync(int patientId)
    {
        return await _context.Visits
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitDate)
            .ToListAsync();
    }

    public async Task<Models.Visit> CreateVisitAsync(Models.Visit visit)
    {
        visit.CreatedAt = DateTime.UtcNow;
        visit.UpdatedAt = DateTime.UtcNow;

        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();

        return visit;
    }

    public async Task<Models.Visit> UpdateVisitAsync(Models.Visit visit)
    {
        var existingVisit = await _context.Visits.FindAsync(visit.Id);
        if (existingVisit == null)
        {
            throw new InvalidOperationException($"Visit with ID {visit.Id} not found");
        }

        existingVisit.PatientId = visit.PatientId;
        existingVisit.VisitDate = visit.VisitDate;
        existingVisit.VisitType = visit.VisitType;
        existingVisit.ChiefComplaint = visit.ChiefComplaint;
        existingVisit.Diagnosis = visit.Diagnosis;
        existingVisit.Treatment = visit.Treatment;
        existingVisit.Notes = visit.Notes;
        existingVisit.Status = visit.Status;
        existingVisit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existingVisit;
    }

    public async Task DeleteVisitAsync(int id)
    {
        var visit = await _context.Visits.FindAsync(id);
        if (visit == null)
        {
            throw new InvalidOperationException($"Visit with ID {id} not found");
        }

        _context.Visits.Remove(visit);
        await _context.SaveChangesAsync();
    }
}
