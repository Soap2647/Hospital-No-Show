using HospitalNoShow.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HospitalNoShow.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Polyclinic> Polyclinics => Set<Polyclinic>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<NoShowAnalytics> NoShowAnalytics => Set<NoShowAnalytics>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tüm IEntityTypeConfiguration implementasyonlarını otomatik uygula
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Identity tablolarını özelleştir
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.HasDefaultSchema("hospital");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit fields otomatik güncelleme
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Patient p) p.UpdatedAt = DateTime.UtcNow;
            if (entry.Entity is Doctor d) d.UpdatedAt = DateTime.UtcNow;
            if (entry.Entity is Appointment a) a.UpdatedAt = DateTime.UtcNow;
            if (entry.Entity is MedicalHistory m) m.UpdatedAt = DateTime.UtcNow;
            if (entry.Entity is NoShowAnalytics n) n.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
