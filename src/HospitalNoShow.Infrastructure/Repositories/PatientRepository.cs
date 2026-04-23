using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Interfaces;
using HospitalNoShow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalNoShow.Infrastructure.Repositories;

public class PatientRepository(ApplicationDbContext context)
    : BaseRepository<Patient>(context), IPatientRepository
{
    public async Task<Patient?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task<Patient?> GetByIdentityNumberAsync(string identityNumber, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdentityNumber == identityNumber, cancellationToken);

    public async Task<Patient?> GetWithAppointmentsAsync(int patientId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
                    .ThenInclude(d => d.User)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.NoShowAnalytics)
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

    public async Task<Patient?> GetWithMedicalHistoryAsync(int patientId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.MedicalHistories)
                .ThenInclude(m => m.Medications)
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

    public async Task<Patient?> GetFullProfileAsync(int patientId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.User)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.NoShowAnalytics)
            .Include(p => p.MedicalHistories)
                .ThenInclude(m => m.Medications)
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

    public async Task<IReadOnlyList<Patient>> GetHighRiskPatientsAsync(
        double minNoShowRate,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(p => p.TotalAppointments > 0 &&
                        (double)p.NoShowCount / p.TotalAppointments >= minNoShowRate)
            .OrderByDescending(p => (double)p.NoShowCount / p.TotalAppointments)
            .ToListAsync(cancellationToken);

    public async Task UpdateNoShowStatsAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var patient = await DbSet.FindAsync([patientId], cancellationToken);
        if (patient is null) return;

        var stats = await Context.Appointments
            .Where(a => a.PatientId == patientId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                NoShows = g.Count(a => a.Status == Domain.Enums.AppointmentStatus.NoShow)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stats is not null)
        {
            patient.TotalAppointments = stats.Total;
            patient.NoShowCount = stats.NoShows;
            patient.UpdatedAt = DateTime.UtcNow;
        }
    }
}
