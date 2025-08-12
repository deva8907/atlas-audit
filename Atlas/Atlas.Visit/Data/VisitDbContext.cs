using Atlas.Audit.Interceptors;
using Atlas.Visit.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Visit.Data;

public class VisitDbContext : DbContext
{
    private readonly AuditInterceptor _auditInterceptor;

    public VisitDbContext(DbContextOptions<VisitDbContext> options, AuditInterceptor auditInterceptor)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    public DbSet<Models.Visit> Visits { get; set; }

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
        modelBuilder.Entity<Models.Visit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PatientId).IsRequired();
            entity.Property(e => e.VisitDate).IsRequired();
            entity.Property(e => e.VisitType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ChiefComplaint).HasMaxLength(500);
            entity.Property(e => e.Diagnosis).HasMaxLength(500);
            entity.Property(e => e.Treatment).HasMaxLength(1000);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired();
            
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.VisitDate);
            entity.HasIndex(e => e.Status);
        });
    }
}
