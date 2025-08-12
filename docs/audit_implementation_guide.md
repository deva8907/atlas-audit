## Implementation Checklist

### Phase 1: Core Audit Infrastructure
- [ ] Create Atlas.Audit project with core interfaces and models
- [ ] Implement audit data structure (`AuditData`, `AuditOperation`)
- [ ] Create strategy pattern interface (`IAuditDataStrategy`)
- [ ] Implement base strategy class (`BaseAuditDataStrategy<T>`)
- [ ] Implement strategy factory with strategy collection
- [ ] Create audit service for database persistence
- [ ] Create audit interceptor for EF Core integration with error handling

### Phase 2: Patient Domain (EF Core + Raw SQL)
- [ ] Create Atlas.Patient project with Patient entity implementing IAuditable
- [ ] Implement PatientAuditDataStrategy in Atlas.Patient
- [ ] Create PatientDbContext with EF Core audit interceptor
- [ ] Implement PatientService with EF Core operations
- [ ] Add Raw SQL operations with manual audit capture in DbContext
- [ ] Create PatientController with CRUD endpoints
- [ ] Register Patient audit strategy in Program.cs

### Phase 3: Visit Domain (EF Core)
- [ ] Create Atlas.Visit project with Visit entity implementing IAuditable
- [ ] Implement VisitAuditDataStrategy in Atlas.Visit
- [ ] Create VisitDbContext with EF Core audit interceptor
- [ ] Implement VisitService with EF Core operations
- [ ] Create VisitController with CRUD endpoints
- [ ] Register Visit audit strategy in Program.cs

### Phase 4: API Integration
- [ ] Set up Atlas.API project with dependency injection
- [ ] Configure audit interceptors and services
- [ ] Register domain-specific audit strategies in factory
- [ ] Configure database connections for all projects
- [ ] Test end-to-end audit functionality

## Database Schema

### Audit Table Structure

```sql
CREATE TABLE AuditLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(128) NOT NULL,
    EntityId NVARCHAR(50) NOT NULL,
    Operation NVARCHAR(10) NOT NULL, -- 'Insert', 'Update', 'Delete'
    OldValues NVARCHAR(MAX), -- JSON
    NewValues NVARCHAR(MAX), -- JSON
    UserId NVARCHAR(50),
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    INDEX IX_AuditLog_TableName_EntityId (TableName, EntityId),
    INDEX IX_AuditLog_Timestamp (Timestamp DESC),
    INDEX IX_AuditLog_UserId (UserId),
    INDEX IX_AuditLog_Operation (Operation)
);
```

### Sample Entity Models

```csharp
// Example entities for reference - implement similar structures in domain projects

// Atlas.Patient/Models/Patient.cs
public class Patient : IAuditable
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string MedicalRecordNumber { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Atlas.Visit/Models/Visit.cs
public class Visit : IAuditable
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public DateTime VisitDate { get; set; }
    public string VisitType { get; set; }
    public string ChiefComplaint { get; set; }
    public string Diagnosis { get; set; }
    public string Treatment { get; set; }
    public string Notes { get; set; }
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
```# Audit System Implementation Guide for .NET 8 - POC

## Requirements Summary

### Core Requirements
1. **Change Tracking with EF Core**: Use change tracker API in EF Core to implement auditing with before-after pattern
2. **Separate Audit Service**: Dedicated service for inserting audit table changes into database
3. **Raw SQL Query Support**: Manually capture audit data while running raw SQL queries
4. **Domain Object Strategy**: Constructing audit data from domain objects using a scalable modular approach with strategy pattern

### Technical Goals
- Modular monolith architecture with separate domain projects
- Scalable architecture that can handle N number of tables
- JSON-based audit schema with old and new values
- Generic audit functionality independent of domain projects
- Support for EF Core operations and Raw SQL

## Project Structure

```
Atlas.API/
├── Controllers/
├── Program.cs
└── appsettings.json

Atlas.Audit/
├── Interfaces/
│   └── IAuditDataStrategy.cs
├── Models/
│   └── AuditData.cs
├── Services/
│   ├── AuditDataStrategyFactory.cs
│   ├── BaseAuditDataStrategy.cs
│   └── AuditService.cs
└── Interceptors/
    └── AuditInterceptor.cs

Atlas.Patient/
├── Models/
│   └── Patient.cs
├── Controllers/
│   └── PatientController.cs
├── Services/
│   └── PatientService.cs
├── Strategies/
│   └── PatientAuditDataStrategy.cs
└── Data/
    └── PatientDbContext.cs

Atlas.Visit/
├── Models/
│   └── Visit.cs
├── Controllers/
│   └── VisitController.cs
├── Services/
│   └── VisitService.cs
├── Strategies/
│   └── VisitAuditDataStrategy.cs
└── Data/
    └── VisitDbContext.cs
```

## Implementation Architecture

### 1. Core Audit Interfaces (Atlas.Audit)

```csharp
// Interfaces/IAuditDataStrategy.cs
public interface IAuditDataStrategy
{
    AuditData ConstructAuditData(object oldEntity, object newEntity, AuditOperation operation, string userId);
    Type EntityType { get; }
}

public interface IAuditable
{
    // Marker interface for auditable entities
}
```

### 2. Audit Data Models (Atlas.Audit)

```csharp
// Models/AuditData.cs
public class AuditData
{
    public string TableName { get; set; }
    public string EntityId { get; set; }
    public AuditOperation Operation { get; set; }
    public Dictionary<string, object> OldValues { get; set; }
    public Dictionary<string, object> NewValues { get; set; }
    public string UserId { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum AuditOperation
{
    Insert,
    Update,
    Delete
}
```

### 3. Strategy Factory Implementation (Atlas.Audit)

```csharp
// Services/AuditDataStrategyFactory.cs
public class AuditDataStrategyFactory
{
    private readonly Dictionary<Type, IAuditDataStrategy> _strategies;

    public AuditDataStrategyFactory(IEnumerable<IAuditDataStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.EntityType, s => s);
    }

    public IAuditDataStrategy GetStrategy(Type entityType)
    {
        if (_strategies.TryGetValue(entityType, out var strategy))
        {
            return strategy;
        }

        throw new InvalidOperationException($"No audit strategy found for entity type {entityType.Name}");
    }

    public bool HasStrategy(Type entityType)
    {
        return _strategies.ContainsKey(entityType);
    }
}
```

### 4. Base Strategy Implementation (Atlas.Audit)

```csharp
// Services/BaseAuditDataStrategy.cs
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
```

### 5. Audit Service (Atlas.Audit)

```csharp
// Services/AuditService.cs
public interface IAuditService
{
    Task SaveAuditDataAsync(AuditData auditData);
}

public class AuditService : IAuditService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IServiceProvider serviceProvider, ILogger<AuditService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SaveAuditDataAsync(AuditData auditData)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            
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

            // Save to database using your preferred data access method
            // This could use a separate DbContext, Dapper, or any other data access approach
            await SaveToDatabase(auditRecord, scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit data for {TableName} with ID {EntityId}", 
                auditData.TableName, auditData.EntityId);
            // Don't rethrow - auditing failures shouldn't break the main operation
        }
    }

    private async Task SaveToDatabase(object auditRecord, IServiceScope scope)
    {
        // Implementation depends on your data access pattern
        // Example using a connection string and simple SQL
        var connectionString = scope.ServiceProvider.GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO AuditLog (TableName, EntityId, Operation, OldValues, NewValues, UserId, Timestamp)
            VALUES (@TableName, @EntityId, @Operation, @OldValues, @NewValues, @UserId, @Timestamp)";

        await connection.ExecuteAsync(sql, auditRecord);
    }
}
```

### 6. EF Core Audit Interceptor (Atlas.Audit)

```csharp
// Interceptors/AuditInterceptor.cs
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly AuditDataStrategyFactory _strategyFactory;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditInterceptor> _logger;

    public AuditInterceptor(
        AuditDataStrategyFactory strategyFactory,
        IAuditService auditService,
        ILogger<AuditInterceptor> logger)
    {
        _strategyFactory = strategyFactory;
        _auditService = auditService;
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var auditEntries = CaptureAuditData(eventData.Context);
        eventData.Context.Items["AuditEntries"] = auditEntries;
        
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context.Items.TryGetValue("AuditEntries", out var auditEntriesObj) && 
            auditEntriesObj is List<AuditData> auditEntries)
        {
            // Process audit entries asynchronously to avoid blocking the main operation
            _ = Task.Run(async () =>
            {
                foreach (var auditData in auditEntries)
                {
                    try
                    {
                        await _auditService.SaveAuditDataAsync(auditData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save audit entry for {TableName}", auditData.TableName);
                    }
                }
            });
        }
        
        return base.SavedChanges(eventData, result);
    }

    private List<AuditData> CaptureAuditData(DbContext context)
    {
        var auditDataList = new List<AuditData>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable && 
                (entry.State == EntityState.Added || 
                 entry.State == EntityState.Modified || 
                 entry.State == EntityState.Deleted))
            {
                var entityType = entry.Entity.GetType();
                
                if (!_strategyFactory.HasStrategy(entityType))
                {
                    continue; // Skip entities without audit strategies
                }

                try
                {
                    var strategy = _strategyFactory.GetStrategy(entityType);

                    var operation = entry.State switch
                    {
                        EntityState.Added => AuditOperation.Insert,
                        EntityState.Modified => AuditOperation.Update,
                        EntityState.Deleted => AuditOperation.Delete,
                        _ => throw new InvalidOperationException($"Unsupported entity state: {entry.State}")
                    };

                    var oldEntity = operation == AuditOperation.Insert ? null : entry.Entity;
                    var newEntity = operation == AuditOperation.Delete ? null : entry.Entity;

                    var auditData = strategy.ConstructAuditData(oldEntity, newEntity, operation, "system");
                    auditDataList.Add(auditData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to capture audit data for entity type {EntityType}", entityType.Name);
                    // Continue processing other entities
                }
            }
        }

        return auditDataList;
    }
}
```

## Domain-Specific Strategy Examples

### Example: Patient Entity Strategy (Atlas.Patient)

```csharp
// Atlas.Patient/Strategies/PatientAuditDataStrategy.cs
using Atlas.Audit.Models;
using Atlas.Audit.Services;

namespace Atlas.Patient.Strategies;

public class PatientAuditDataStrategy : BaseAuditDataStrategy<Models.Patient>
{
    public PatientAuditDataStrategy(ILogger<PatientAuditDataStrategy> logger) 
        : base(logger) { }

    protected override string GetTableName() => "Patients";

    protected override string GetEntityId(Models.Patient entity) => entity?.Id.ToString();

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
```

## API Project Configuration (Atlas.API)

### Program.cs

```csharp
// Atlas.API/Program.cs
using Atlas.Audit.Interceptors;
using Atlas.Audit.Interfaces;
using Atlas.Audit.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database connections
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register Audit services
builder.Services.AddScoped<IAuditService, AuditService>();

// Register audit strategies - example with Patient and Visit strategies
// var auditStrategies = new List<IAuditDataStrategy>
// {
//     new PatientAuditDataStrategy(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PatientAuditDataStrategy>()),
//     new VisitAuditDataStrategy(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<VisitAuditDataStrategy>())
// };

// builder.Services.AddSingleton(new AuditDataStrategyFactory(auditStrategies));
builder.Services.AddScoped<AuditInterceptor>();

// Domain services configuration will be added here when implementing domains

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Implementation Checklist

### Phase 1: Core Audit Infrastructure
- [ ] Create Atlas.Audit project with core interfaces and models
- [ ] Implement audit data structure (`AuditData`, `AuditOperation`)
- [ ] Create strategy pattern interface (`IAuditDataStrategy`)
- [ ] Implement base strategy class (`BaseAuditDataStrategy<T>`)
- [ ] Implement strategy factory with strategy collection
- [ ] Create audit service for database persistence
- [ ] Create audit interceptor for EF Core integration with error handling

### Phase 2: Patient Domain (EF Core + Raw SQL)
- [ ] Create Atlas.Patient project with Patient entity
- [ ] Implement PatientAuditDataStrategy in Atlas.Patient
- [ ] Create PatientDbContext with EF Core audit interceptor
- [ ] Implement PatientService with EF Core operations
- [ ] Add Raw SQL operations with manual audit capture
- [ ] Create PatientController with CRUD endpoints

### Phase 3: Visit Domain (EF Core)
- [ ] Create Atlas.Visit project with Visit entity
- [ ] Implement VisitAuditDataStrategy in Atlas.Visit
- [ ] Create VisitDbContext with EF Core audit interceptor
- [ ] Implement VisitService with EF Core operations
- [ ] Create VisitController with CRUD endpoints