using Atlas.Audit.Interfaces;
using Atlas.Audit.Models;
using Atlas.Audit.Services;
using Atlas.Patient.Data;
using Atlas.Patient.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Atlas.Patient.Services;

public interface IPatientService
{
    Task<IEnumerable<Models.Patient>> GetAllPatientsAsync();
    Task<Models.Patient?> GetPatientByIdAsync(int id);
    Task<Models.Patient> CreatePatientAsync(Models.Patient patient);
    Task<Models.Patient> UpdatePatientAsync(Models.Patient patient);
    Task DeletePatientAsync(int id);
    Task<IEnumerable<Models.Patient>> GetPatientsByRawSqlAsync(string searchTerm);
}

public class PatientService : IPatientService
{
    private readonly PatientDbContext _context;
    private readonly IAuditService _auditService;
    private readonly AuditDataStrategyFactory _strategyFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        PatientDbContext context,
        IAuditService auditService,
        AuditDataStrategyFactory strategyFactory,
        IConfiguration configuration,
        ILogger<PatientService> logger)
    {
        _context = context;
        _auditService = auditService;
        _strategyFactory = strategyFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IEnumerable<Models.Patient>> GetAllPatientsAsync()
    {
        return await _context.Patients.ToListAsync();
    }

    public async Task<Models.Patient?> GetPatientByIdAsync(int id)
    {
        return await _context.Patients.FindAsync(id);
    }

    public async Task<Models.Patient> CreatePatientAsync(Models.Patient patient)
    {
        patient.CreatedAt = DateTime.UtcNow;
        patient.UpdatedAt = DateTime.UtcNow;

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return patient;
    }

    public async Task<Models.Patient> UpdatePatientAsync(Models.Patient patient)
    {
        var existingPatient = await _context.Patients.FindAsync(patient.Id);
        if (existingPatient == null)
        {
            throw new InvalidOperationException($"Patient with ID {patient.Id} not found");
        }

        existingPatient.FirstName = patient.FirstName;
        existingPatient.LastName = patient.LastName;
        existingPatient.DateOfBirth = patient.DateOfBirth;
        existingPatient.MedicalRecordNumber = patient.MedicalRecordNumber;
        existingPatient.PhoneNumber = patient.PhoneNumber;
        existingPatient.Email = patient.Email;
        existingPatient.Address = patient.Address;
        existingPatient.IsActive = patient.IsActive;
        existingPatient.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existingPatient;
    }

    public async Task DeletePatientAsync(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null)
        {
            throw new InvalidOperationException($"Patient with ID {id} not found");
        }

        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Models.Patient>> GetPatientsByRawSqlAsync(string searchTerm)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
        }

        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Ensure the Patients table exists
        await EnsurePatientsTableExistsAsync(connection);

        const string sql = @"
            SELECT Id, FirstName, LastName, DateOfBirth, MedicalRecordNumber, 
                   PhoneNumber, Email, Address, IsActive, CreatedAt, UpdatedAt
            FROM Patients 
            WHERE FirstName LIKE @SearchTerm OR LastName LIKE @SearchTerm OR MedicalRecordNumber LIKE @SearchTerm";

        var patients = new List<Models.Patient>();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var patient = new Models.Patient
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                MedicalRecordNumber = reader.GetString(reader.GetOrdinal("MedicalRecordNumber")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : reader.GetString(reader.GetOrdinal("Address")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };

            patients.Add(patient);

            // Manually capture audit data for raw SQL operations
            await CaptureRawSqlAuditDataAsync(patient, "SELECT", "system");
        }

        return patients;
    }

    private async Task EnsurePatientsTableExistsAsync(SqliteConnection connection)
    {
        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS Patients (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                DateOfBirth TEXT NOT NULL,
                MedicalRecordNumber TEXT NOT NULL UNIQUE,
                PhoneNumber TEXT,
                Email TEXT,
                Address TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )";

        using var command = new SqliteCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CaptureRawSqlAuditDataAsync(Models.Patient patient, string operation, string userId)
    {
        try
        {
            if (_strategyFactory.HasStrategy(typeof(Models.Patient)))
            {
                var strategy = _strategyFactory.GetStrategy(typeof(Models.Patient));
                var auditOperation = operation.ToUpper() switch
                {
                    "SELECT" => AuditOperation.Insert, // Treat SELECT as a read operation, no audit needed for reads
                    "INSERT" => AuditOperation.Insert,
                    "UPDATE" => AuditOperation.Update,
                    "DELETE" => AuditOperation.Delete,
                    _ => throw new InvalidOperationException($"Unsupported operation: {operation}")
                };

                // For SELECT operations, we don't need to audit as it's just reading data
                if (auditOperation != AuditOperation.Insert || operation.ToUpper() != "SELECT")
                {
                    var auditData = strategy.ConstructAuditData(null, patient, auditOperation, userId);
                    await _auditService.SaveAuditDataAsync(auditData);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture audit data for raw SQL operation on Patient {PatientId}", patient.Id);
            // Don't rethrow - audit failures shouldn't break the main operation
        }
    }
}
