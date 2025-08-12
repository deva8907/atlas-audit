using Atlas.Audit.Interceptors;
using Atlas.Patient.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Patient.Data;

public class PatientDbContext : DbContext
{
    private readonly AuditInterceptor _auditInterceptor;

    public PatientDbContext(DbContextOptions<PatientDbContext> options, AuditInterceptor auditInterceptor)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    public DbSet<Models.Patient> Patients { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=atlas.db");
        }

        // Add the audit interceptor
        optionsBuilder.AddInterceptors(_auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MedicalRecordNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            
            entity.HasIndex(e => e.MedicalRecordNumber).IsUnique();
            entity.HasIndex(e => e.Email);
        });
    }
}
