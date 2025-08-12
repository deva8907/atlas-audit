using Atlas.Audit.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Atlas.Audit.Services;

public interface IAuditService
{
    Task SaveAuditDataAsync(AuditData auditData);
}

public class AuditService : IAuditService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IConfiguration configuration, ILogger<AuditService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SaveAuditDataAsync(AuditData auditData)
    {
        try
        {
            // Create a simple audit record for database storage
            var auditRecord = new
            {
                TableName = auditData.TableName,
                EntityId = auditData.EntityId,
                Operation = auditData.Operation.ToString(),
                OldValues = auditData.OldValues != null ? JsonSerializer.Serialize(auditData.OldValues) : null,
                NewValues = auditData.NewValues != null ? JsonSerializer.Serialize(auditData.NewValues) : null,
                UserId = auditData.UserId,
                Timestamp = auditData.Timestamp
            };

            // Save to database using SQLite
            await SaveToDatabase(auditRecord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit data for {TableName} with ID {EntityId}", 
                auditData.TableName, auditData.EntityId);
            // Don't rethrow - auditing failures shouldn't break the main operation
        }
    }

    private async Task SaveToDatabase(object auditRecord)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Connection string 'DefaultConnection' not found in configuration");
            return;
        }

        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Ensure the audit table exists
        await EnsureAuditTableExistsAsync(connection);

        const string sql = @"
            INSERT INTO AuditLog (TableName, EntityId, Operation, OldValues, NewValues, UserId, Timestamp)
            VALUES (@TableName, @EntityId, @Operation, @OldValues, @NewValues, @UserId, @Timestamp)";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@TableName", auditRecord.GetType().GetProperty("TableName")?.GetValue(auditRecord) ?? DBNull.Value);
        command.Parameters.AddWithValue("@EntityId", auditRecord.GetType().GetProperty("EntityId")?.GetValue(auditRecord) ?? DBNull.Value);
        command.Parameters.AddWithValue("@Operation", auditRecord.GetType().GetProperty("Operation")?.GetValue(auditRecord) ?? DBNull.Value);
        command.Parameters.AddWithValue("@OldValues", auditRecord.GetType().GetProperty("OldValues")?.GetValue(auditRecord) ?? DBNull.Value);
        command.Parameters.AddWithValue("@NewValues", auditRecord.GetType().GetProperty("NewValues")?.GetValue(auditRecord) ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserId", auditRecord.GetType().GetProperty("UserId")?.GetValue(auditRecord) ?? DBNull.Value);
        command.Parameters.AddWithValue("@Timestamp", auditRecord.GetType().GetProperty("Timestamp")?.GetValue(auditRecord) ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureAuditTableExistsAsync(SqliteConnection connection)
    {
        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS AuditLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TableName TEXT NOT NULL,
                EntityId TEXT NOT NULL,
                Operation TEXT NOT NULL,
                OldValues TEXT,
                NewValues TEXT,
                UserId TEXT,
                Timestamp TEXT NOT NULL
            )";

        using var command = new SqliteCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
