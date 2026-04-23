using HospitalNoShow.Domain.Entities;

namespace HospitalNoShow.Domain.Interfaces;

public interface IDoctorRepository : IRepository<Doctor>
{
    Task<Doctor?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Doctor?> GetWithSchedulesAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<Doctor?> GetWithAppointmentsAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(string specialty, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Doctor>> GetByPolyclinicAsync(int polyclinicId, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAtAsync(int doctorId, DateTime date, TimeOnly time, CancellationToken cancellationToken = default);
    Task<int> GetDailyAppointmentCountAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);
}
