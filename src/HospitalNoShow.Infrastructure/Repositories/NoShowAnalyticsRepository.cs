using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Domain.Interfaces;
using HospitalNoShow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalNoShow.Infrastructure.Repositories;

public class NoShowAnalyticsRepository(ApplicationDbContext context)
    : BaseRepository<NoShowAnalytics>(context), INoShowAnalyticsRepository
{
    public async Task<NoShowAnalytics?> GetByAppointmentIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(n => n.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(n => n.Appointment)
                .ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(n => n.AppointmentId == appointmentId, cancellationToken);

    public async Task<IReadOnlyList<NoShowAnalytics>> GetHighRiskAppointmentsAsync(
        double minRiskScore,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(n => n.Appointment)
                .ThenInclude(a => a.Patient)
                    .ThenInclude(p => p.User)
            .Include(n => n.Appointment)
                .ThenInclude(a => a.Doctor)
                    .ThenInclude(d => d.User)
            .Where(n =>
                n.RiskScore >= minRiskScore &&
                n.Appointment.Status == AppointmentStatus.Scheduled)
            .OrderByDescending(n => n.RiskScore)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NoShowAnalytics>> GetPendingReminderSmsAsync(
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(n => n.Appointment)
                .ThenInclude(a => a.Patient)
            .Where(n =>
                !n.IsReminderSent &&
                n.Appointment.Status == AppointmentStatus.Scheduled &&
                n.Appointment.AppointmentDate > DateTime.UtcNow &&
                n.Appointment.AppointmentDate <= DateTime.UtcNow.AddDays(2))
            .ToListAsync(cancellationToken);

    public async Task<double> GetAverageRiskScoreForDoctorAsync(
        int doctorId,
        CancellationToken cancellationToken = default)
    {
        var result = await DbSet.AsNoTracking()
            .Where(n => n.Appointment.DoctorId == doctorId)
            .AverageAsync(n => (double?)n.RiskScore, cancellationToken);

        return result ?? 0.0;
    }
}
