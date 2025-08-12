using Atlas.Audit.Interceptors;
using Atlas.Audit.Interfaces;
using Atlas.Audit.Services;
using Atlas.Patient.Data;
using Atlas.Patient.Services;
using Atlas.Patient.Strategies;
using Atlas.Visit.Data;
using Atlas.Visit.Services;
using Atlas.Visit.Strategies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database connection string
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=atlas.db";

// Register Audit services
builder.Services.AddScoped<IAuditService, AuditService>();

// Register audit strategies
builder.Services.AddScoped<IAuditDataStrategy, PatientAuditDataStrategy>();
builder.Services.AddScoped<IAuditDataStrategy, VisitAuditDataStrategy>();

// Register audit strategy factory
builder.Services.AddScoped<AuditDataStrategyFactory>(serviceProvider =>
{
    var strategies = serviceProvider.GetServices<IAuditDataStrategy>();
    return new AuditDataStrategyFactory(strategies);
});

// Register audit interceptor
builder.Services.AddScoped<AuditInterceptor>();

// Register Patient domain services
builder.Services.AddDbContext<PatientDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("Atlas.API")));

builder.Services.AddScoped<IPatientService, PatientService>();

// Register Visit domain services
builder.Services.AddDbContext<VisitDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("Atlas.API")));

builder.Services.AddScoped<IVisitService, VisitService>();

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
