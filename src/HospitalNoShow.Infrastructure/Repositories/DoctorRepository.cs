using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Domain.Interfaces;
using HospitalNoShow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalNoShow.Infrastructure.Repositories;

public class DoctorRepository(ApplicationDbContext context)
    : BaseRepository<Doctor>(context), IDoctorRepository
{
    public override async Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Polyclinic)
            .ToListAsync(cancellationToken);

    public async Task<Doctor?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Polyclinic)
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

    public async Task<Doctor?> GetWithSchedulesAsync(int doctorId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(d => d.User)
            .Include(d => d.Polyclinic)
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);

    public async Task<Doctor?> GetWithAppointmentsAsync(int doctorId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(d => d.User)
            .Include(d => d.Polyclinic)
            .Include(d => d.Appointments)
                .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);

    public async Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(
        string specialty,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Polyclinic)
            .Where(d => d.Specialty.Contains(specialty))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Doctor>> GetByPolyclinicAsync(
        int polyclinicId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(d => d.User)
            .Where(d => d.PolyclinicId == polyclinicId)
            .ToListAsync(cancellationToken);

    public async Task<bool> IsAvailableAtAsync(
        int doctorId,
        DateTime date,
        TimeOnly time,
        CancellationToken cancellationToken = default)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var dayOfWeek = date.DayOfWeek;

        // Hafta sonu kontrolü
        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
            return false;

        // Çalışma saatlerini kontrol et
        var schedule = await Context.DoctorSchedules
            .FirstOrDefaultAsync(s =>
                s.DoctorId == doctorId &&
                s.DayOfWeek == dayOfWeek &&
                s.IsAvailable,
                cancellationToken);

        if (schedule != null)
        {
            // Programı varsa, saatin program içinde olup olmadığını kontrol et
            if (!(schedule.StartTime <= time && schedule.EndTime > time))
                return false;
        }
        else
        {
            // Program yoksa varsayılan 09:00-17:00 arası kabul et
            var defaultStart = new TimeOnly(9, 0);
            var defaultEnd   = new TimeOnly(17, 0);
            if (!(defaultStart <= time && defaultEnd > time))
                return false;
        }

        // Çakışan randevu var mı kontrol et
        var hasConflict = await Context.Appointments
            .AnyAsync(a =>
                a.DoctorId == doctorId &&
                DateOnly.FromDateTime(a.AppointmentDate) == dateOnly &&
                a.AppointmentTime == time &&
                a.Status != AppointmentStatus.Cancelled,
                cancellationToken);

        return !hasConflict;
    }

    public async Task<int> GetDailyAppointmentCountAsync(
        int doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
        => await Context.Appointments
            .CountAsync(a =>
                a.DoctorId == doctorId &&
                DateOnly.FromDateTime(a.AppointmentDate) == date &&
                a.Status != AppointmentStatus.Cancelled,
                cancellationToken);
}
