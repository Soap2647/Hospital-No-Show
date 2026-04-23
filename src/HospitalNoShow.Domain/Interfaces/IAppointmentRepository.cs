using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;

namespace HospitalNoShow.Domain.Interfaces;

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<Appointment?> GetWithDetailsAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByDoctorAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByDoctorAndDateAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetByStatusAsync(AppointmentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetAllForAdminAsync(AppointmentStatus? status = null, int limit = 10000, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetUpcomingByPatientAsync(int patientId, int days = 7, CancellationToken cancellationToken = default);
    Task<int> GetPatientNoShowCountAsync(int patientId, CancellationToken cancellationToken = default);
    Task<int> GetPatientTotalAppointmentCountAsync(int patientId, CancellationToken cancellationToken = default);
    Task<int> GetSlotOrderInDayAsync(int doctorId, DateOnly date, TimeOnly time, CancellationToken cancellationToken = default);
    Task<int> GetTotalSlotsForDayAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);
}
