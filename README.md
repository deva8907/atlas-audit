# Atlas Audit System - .NET 8 POC

A comprehensive audit system implementation demonstrating change tracking with EF Core, raw SQL support, and a scalable strategy pattern architecture.

## 🏗️ Architecture Overview

This project implements a **modular monolith** architecture with separate domain projects:

- **Atlas.Audit**: Core audit infrastructure with strategy pattern
- **Atlas.Patient**: Patient domain with EF Core + Raw SQL audit support
- **Atlas.Visit**: Visit domain with EF Core audit support
- **Atlas.API**: Web API integration layer

## 🎯 Key Features

### ✅ Core Audit Infrastructure
- **Strategy Pattern**: Domain-specific audit data construction
- **EF Core Integration**: Automatic audit capture via interceptors
- **Raw SQL Support**: Manual audit capture for custom queries
- **Error Resilience**: Audit failures don't break main operations
- **JSON-based Storage**: Flexible audit data schema

### ✅ Scalable Design
- **Modular Architecture**: Easy to add new domains
- **Strategy Factory**: Centralized audit strategy management
- **Generic Base Classes**: Reusable audit functionality
- **Single Connection String**: Shared database for all domains

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- SQLite (included with EF Core)

### Running the Application

1. **Build the solution**:
   ```bash
   dotnet build
   ```

2. **Run the API**:
   ```bash
   cd Atlas.API
   dotnet run
   ```

3. **Access Swagger UI**:
   ```
   https://localhost:7001/swagger
   ```

## 📊 Database Schema

### AuditLog Table
```sql
CREATE TABLE AuditLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TableName TEXT NOT NULL,
    EntityId TEXT NOT NULL,
    Operation TEXT NOT NULL, -- 'Insert', 'Update', 'Delete'
    OldValues TEXT, -- JSON
    NewValues TEXT, -- JSON
    UserId TEXT,
    Timestamp TEXT NOT NULL
);
```

### Domain Tables
- **Patients**: Patient information with medical records
- **Visits**: Patient visit records with status tracking

## 🔧 API Endpoints

### Patient Management
- `GET /api/patient` - Get all patients
- `GET /api/patient/{id}` - Get patient by ID
- `GET /api/patient/search?term={term}` - Search patients (Raw SQL)
- `POST /api/patient` - Create new patient
- `PUT /api/patient/{id}` - Update patient
- `DELETE /api/patient/{id}` - Delete patient

### Visit Management
- `GET /api/visit` - Get all visits
- `GET /api/visit/{id}` - Get visit by ID
- `GET /api/visit/patient/{patientId}` - Get visits by patient
- `POST /api/visit` - Create new visit
- `PUT /api/visit/{id}` - Update visit
- `DELETE /api/visit/{id}` - Delete visit

## 🧪 Testing the Audit System

### 1. Create a Patient (EF Core Audit)
```bash
curl -X POST "https://localhost:7001/api/patient" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "dateOfBirth": "1990-01-01",
    "medicalRecordNumber": "MRN001",
    "phoneNumber": "555-1234",
    "email": "john.doe@email.com",
    "address": "123 Main St",
    "isActive": true
  }'
```

### 2. Update a Patient (EF Core Audit)
```bash
curl -X PUT "https://localhost:7001/api/patient/1" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "firstName": "John",
    "lastName": "Smith",
    "dateOfBirth": "1990-01-01",
    "medicalRecordNumber": "MRN001",
    "phoneNumber": "555-5678",
    "email": "john.smith@email.com",
    "address": "456 Oak Ave",
    "isActive": true
  }'
```

### 3. Search Patients (Raw SQL Audit)
```bash
curl -X GET "https://localhost:7001/api/patient/search?term=John"
```

### 4. Create a Visit (EF Core Audit)
```bash
curl -X POST "https://localhost:7001/api/visit" \
  -H "Content-Type: application/json" \
  -d '{
    "patientId": 1,
    "visitDate": "2024-01-15T10:00:00Z",
    "visitType": "Checkup",
    "chiefComplaint": "Annual physical",
    "diagnosis": "Healthy",
    "treatment": "No treatment needed",
    "notes": "Patient in good health",
    "status": 0
  }'
```

### 5. View Audit Log
The audit data is automatically captured and stored in the `AuditLog` table. You can query it directly:

```sql
SELECT * FROM AuditLog ORDER BY Timestamp DESC;
```

## 🏛️ Architecture Patterns

### Strategy Pattern
```csharp
// Domain-specific audit strategies
public class PatientAuditDataStrategy : BaseAuditDataStrategy<Patient>
public class VisitAuditDataStrategy : BaseAuditDataStrategy<Visit>
```

### Factory Pattern
```csharp
// Centralized strategy management
public class AuditDataStrategyFactory
```

### Interceptor Pattern
```csharp
// EF Core audit integration
public class AuditInterceptor : SaveChangesInterceptor
```

## 🔒 Error Handling

- **Audit Failures**: Don't break main operations
- **Comprehensive Logging**: All errors are logged
- **Graceful Degradation**: System continues working even if audit fails
- **Async Processing**: Audit operations don't block main transactions

## 📈 Scalability Features

- **Easy Domain Addition**: Just implement `IAuditDataStrategy`
- **Generic Base Classes**: Reusable audit functionality
- **Modular Design**: Independent domain projects
- **Strategy Registration**: Automatic discovery and registration

## 🛠️ Development

### Adding a New Domain

1. Create domain project (e.g., `Atlas.Order`)
2. Implement entity with `IAuditable` interface
3. Create audit strategy extending `BaseAuditDataStrategy<T>`
4. Register in `Program.cs`
5. Add controllers and services

### Customizing Audit Data

Override the `ExtractValues` method in your strategy:

```csharp
protected override Dictionary<string, object> ExtractValues(MyEntity entity)
{
    return new Dictionary<string, object>
    {
        ["Id"] = entity.Id,
        ["Name"] = entity.Name,
        // Add your custom fields
    };
}
```

## 📝 Notes

- **User Context**: Currently hardcoded as "system" for POC
- **Database**: SQLite for simplicity, easily changeable to SQL Server
- **Dependencies**: No external dependencies beyond EF Core
- **Performance**: Audit operations are asynchronous and non-blocking

## 🎉 Success!

The audit system is now fully functional with:
- ✅ EF Core automatic audit capture
- ✅ Raw SQL manual audit capture  
- ✅ Scalable strategy pattern architecture
- ✅ Error-resilient design
- ✅ Complete API endpoints
- ✅ Comprehensive documentation
