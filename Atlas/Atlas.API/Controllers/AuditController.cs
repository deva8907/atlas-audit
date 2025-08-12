using Atlas.Audit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Atlas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IConfiguration configuration, ILogger<AuditController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? tableName = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? operation = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "Data Source=atlas.db";
            
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Build the query with filters
            var whereConditions = new List<string>();
            var parameters = new List<SqliteParameter>();

            if (!string.IsNullOrEmpty(tableName))
            {
                whereConditions.Add("TableName = @TableName");
                parameters.Add(new SqliteParameter("@TableName", tableName));
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                whereConditions.Add("EntityId = @EntityId");
                parameters.Add(new SqliteParameter("@EntityId", entityId));
            }

            if (!string.IsNullOrEmpty(operation))
            {
                whereConditions.Add("Operation = @Operation");
                parameters.Add(new SqliteParameter("@Operation", operation));
            }

            if (fromDate.HasValue)
            {
                whereConditions.Add("Timestamp >= @FromDate");
                parameters.Add(new SqliteParameter("@FromDate", fromDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            if (toDate.HasValue)
            {
                whereConditions.Add("Timestamp <= @ToDate");
                parameters.Add(new SqliteParameter("@ToDate", toDate.Value.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            var whereClause = whereConditions.Count > 0 ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM AuditLog {whereClause}";
            using var countCommand = new SqliteCommand(countSql, connection);
            countCommand.Parameters.AddRange(parameters.ToArray());
            var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

            // Get paginated results
            var offset = (page - 1) * pageSize;
            var sql = $@"
                SELECT Id, TableName, EntityId, Operation, OldValues, NewValues, UserId, Timestamp
                FROM AuditLog 
                {whereClause}
                ORDER BY Timestamp DESC
                LIMIT @PageSize OFFSET @Offset";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());
            command.Parameters.Add(new SqliteParameter("@PageSize", pageSize));
            command.Parameters.Add(new SqliteParameter("@Offset", offset));

            var auditLogs = new List<object>();
            using var reader = await command.ExecuteReaderAsync();

            var idxId = reader.GetOrdinal("Id");
            var idxTableName = reader.GetOrdinal("TableName");
            var idxEntityId = reader.GetOrdinal("EntityId");
            var idxOperation = reader.GetOrdinal("Operation");
            var idxOldValues = reader.GetOrdinal("OldValues");
            var idxNewValues = reader.GetOrdinal("NewValues");
            var idxUserId = reader.GetOrdinal("UserId");
            var idxTimestamp = reader.GetOrdinal("Timestamp");

            while (await reader.ReadAsync())
            {
                var auditLog = new
                {
                    Id = reader.IsDBNull(idxId) ? 0 : Convert.ToInt32(reader.GetValue(idxId)),
                    TableName = reader.IsDBNull(idxTableName) ? string.Empty : reader.GetString(idxTableName),
                    EntityId = reader.IsDBNull(idxEntityId) ? string.Empty : reader.GetString(idxEntityId),
                    Operation = reader.IsDBNull(idxOperation) ? string.Empty : reader.GetString(idxOperation),
                    OldValues = reader.IsDBNull(idxOldValues) ? null : JsonSerializer.Deserialize<object>(reader.GetString(idxOldValues)),
                    NewValues = reader.IsDBNull(idxNewValues) ? null : JsonSerializer.Deserialize<object>(reader.GetString(idxNewValues)),
                    UserId = reader.IsDBNull(idxUserId) ? null : reader.GetString(idxUserId),
                    Timestamp = reader.IsDBNull(idxTimestamp) ? string.Empty : reader.GetString(idxTimestamp)
                };
                auditLogs.Add(auditLog);
            }

            var result = new
            {
                Data = auditLogs,
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, "An error occurred while retrieving audit logs");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuditLogById(int id)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "Data Source=atlas.db";
            
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT Id, TableName, EntityId, Operation, OldValues, NewValues, UserId, Timestamp
                FROM AuditLog 
                WHERE Id = @Id";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.Add(new SqliteParameter("@Id", id));

            using var reader = await command.ExecuteReaderAsync();

            var idxId = reader.GetOrdinal("Id");
            var idxTableName = reader.GetOrdinal("TableName");
            var idxEntityId = reader.GetOrdinal("EntityId");
            var idxOperation = reader.GetOrdinal("Operation");
            var idxOldValues = reader.GetOrdinal("OldValues");
            var idxNewValues = reader.GetOrdinal("NewValues");
            var idxUserId = reader.GetOrdinal("UserId");
            var idxTimestamp = reader.GetOrdinal("Timestamp");

            if (await reader.ReadAsync())
            {
                var auditLog = new
                {
                    Id = reader.IsDBNull(idxId) ? 0 : Convert.ToInt32(reader.GetValue(idxId)),
                    TableName = reader.IsDBNull(idxTableName) ? string.Empty : reader.GetString(idxTableName),
                    EntityId = reader.IsDBNull(idxEntityId) ? string.Empty : reader.GetString(idxEntityId),
                    Operation = reader.IsDBNull(idxOperation) ? string.Empty : reader.GetString(idxOperation),
                    OldValues = reader.IsDBNull(idxOldValues) ? null : JsonSerializer.Deserialize<object>(reader.GetString(idxOldValues)),
                    NewValues = reader.IsDBNull(idxNewValues) ? null : JsonSerializer.Deserialize<object>(reader.GetString(idxNewValues)),
                    UserId = reader.IsDBNull(idxUserId) ? null : reader.GetString(idxUserId),
                    Timestamp = reader.IsDBNull(idxTimestamp) ? string.Empty : reader.GetString(idxTimestamp)
                };

                return Ok(auditLog);
            }

            return NotFound($"Audit log with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the audit log");
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetAuditSummary()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "Data Source=atlas.db";
            
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    COUNT(*) as TotalRecords,
                    COUNT(DISTINCT TableName) as UniqueTables,
                    COUNT(DISTINCT EntityId) as UniqueEntities,
                    COUNT(DISTINCT Operation) as UniqueOperations,
                    COUNT(DISTINCT UserId) as UniqueUsers,
                    MIN(Timestamp) as FirstAudit,
                    MAX(Timestamp) as LastAudit
                FROM AuditLog";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var idxTotalRecords = reader.GetOrdinal("TotalRecords");
            var idxUniqueTables = reader.GetOrdinal("UniqueTables");
            var idxUniqueEntities = reader.GetOrdinal("UniqueEntities");
            var idxUniqueOperations = reader.GetOrdinal("UniqueOperations");
            var idxUniqueUsers = reader.GetOrdinal("UniqueUsers");
            var idxFirstAudit = reader.GetOrdinal("FirstAudit");
            var idxLastAudit = reader.GetOrdinal("LastAudit");

            if (await reader.ReadAsync())
            {
                var summary = new
                {
                    TotalRecords = reader.IsDBNull(idxTotalRecords) ? 0 : Convert.ToInt32(reader.GetValue(idxTotalRecords)),
                    UniqueTables = reader.IsDBNull(idxUniqueTables) ? 0 : Convert.ToInt32(reader.GetValue(idxUniqueTables)),
                    UniqueEntities = reader.IsDBNull(idxUniqueEntities) ? 0 : Convert.ToInt32(reader.GetValue(idxUniqueEntities)),
                    UniqueOperations = reader.IsDBNull(idxUniqueOperations) ? 0 : Convert.ToInt32(reader.GetValue(idxUniqueOperations)),
                    UniqueUsers = reader.IsDBNull(idxUniqueUsers) ? 0 : Convert.ToInt32(reader.GetValue(idxUniqueUsers)),
                    FirstAudit = reader.IsDBNull(idxFirstAudit) ? null : reader.GetString(idxFirstAudit),
                    LastAudit = reader.IsDBNull(idxLastAudit) ? null : reader.GetString(idxLastAudit)
                };

                return Ok(summary);
            }

            return Ok(new { TotalRecords = 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit summary");
            return StatusCode(500, "An error occurred while retrieving audit summary");
        }
    }
}
