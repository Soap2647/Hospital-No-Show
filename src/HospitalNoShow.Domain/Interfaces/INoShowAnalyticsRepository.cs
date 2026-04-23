using HospitalNoShow.Domain.Entities;

namespace HospitalNoShow.Domain.Interfaces;

public interface INoShowAnalyticsRepository : IRepository<NoShowAnalytics>
{
    Task<NoShowAnalytics?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NoShowAnalytics>> GetHighRiskAppointmentsAsync(double minRiskScore, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NoShowAnalytics>> GetPendingReminderSmsAsync(CancellationToken cancellationToken = default);
    Task<double> GetAverageRiskScoreForDoctorAsync(int doctorId, CancellationToken cancellationToken = default);
}
