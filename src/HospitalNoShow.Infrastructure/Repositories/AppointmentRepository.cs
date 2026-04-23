using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Domain.Interfaces;
using HospitalNoShow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalNoShow.Infrastructure.Repositories;

public class AppointmentRepository(ApplicationDbContext context)
    : BaseRepository<Appointment>(context), IAppointmentRepository
{
    public async Task<Appointment?> GetWithDetailsAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(a => a.Patient)
                .ThenInclude(p => p.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Polyclinic)
            .Include(a => a.NoShowAnalytics)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetByPatientAsync(
        int patientId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(a => a.Patient)
                .ThenInclude(p => p.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Polyclinic)
            .Include(a => a.NoShowAnalytics)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetByDoctorAsync(
        int doctorId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(a => a.Patient)
                .ThenInclude(p => p.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Polyclinic)
            .Include(a => a.NoShowAnalytics)
            .Where(a => a.DoctorId == doctorId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetByDoctorAndDateAsync(
        int doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Include(a => a.Patient)
                .ThenInclude(p => p.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Polyclinic)
            .Include(a => a.NoShowAnalytics)
            .Where(a => a.DoctorId == doctorId &&
                        DateOnly.FromDateTime(a.AppointmentDate) == date)
            .OrderBy(a => a.AppointmentTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByStatusAsync(
        AppointmentStatus status,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Where(a => a.Status == status)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetAllForAdminAsync(
        AppointmentStatus? status = null,
        int limit = 10000,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor).ThenInclude(d => d.Polyclinic)
            .Include(a => a.NoShowAnalytics)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetUpcomingByPatientAsync(
        int patientId,
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var until = now.AddDays(days);

        return await DbSet.AsNoTracking()
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor).ThenInclude(d => d.Polyclinic)
            .Include(a => a.NoShowAnalytics)
            .Where(a => a.PatientId == patientId &&
                        a.AppointmentDate >= now &&
                        a.AppointmentDate <= until &&
                        a.Status == AppointmentStatus.Scheduled)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPatientNoShowCountAsync(
        int patientId,
        CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(
            a => a.PatientId == patientId && a.Status == AppointmentStatus.NoShow,
            cancellationToken);

    public async Task<int> GetPatientTotalAppointmentCountAsync(
        int patientId,
        CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(
            a => a.PatientId == patientId,
            cancellationToken);

    public async Task<int> GetSlotOrderInDayAsync(
        int doctorId,
        DateOnly date,
        TimeOnly time,
        CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(
            a => a.DoctorId == doctorId &&
                 DateOnly.FromDateTime(a.AppointmentDate) == date &&
                 a.AppointmentTime < time &&
                 a.Status != AppointmentStatus.Cancelled,
            cancellationToken) + 1;

    public async Task<int> GetTotalSlotsForDayAsync(
        int doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(
            a => a.DoctorId == doctorId &&
                 DateOnly.FromDateTime(a.AppointmentDate) == date &&
                 a.Status != AppointmentStatus.Cancelled,
            cancellationToken);
}
