using HospitalNoShow.Domain.Entities;

namespace HospitalNoShow.Domain.Interfaces;

public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Patient?> GetByIdentityNumberAsync(string identityNumber, CancellationToken cancellationToken = default);
    Task<Patient?> GetWithAppointmentsAsync(int patientId, CancellationToken cancellationToken = default);
    Task<Patient?> GetWithMedicalHistoryAsync(int patientId, CancellationToken cancellationToken = default);
    Task<Patient?> GetFullProfileAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Patient>> GetHighRiskPatientsAsync(double minNoShowRate, CancellationToken cancellationToken = default);
    Task UpdateNoShowStatsAsync(int patientId, CancellationToken cancellationToken = default);
}
