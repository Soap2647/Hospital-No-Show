namespace HospitalNoShow.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IPatientRepository Patients { get; }
    IDoctorRepository Doctors { get; }
    IAppointmentRepository Appointments { get; }
    INoShowAnalyticsRepository NoShowAnalytics { get; }
    IRepository<Entities.MedicalHistory> MedicalHistories { get; }
    IRepository<Entities.Medication> Medications { get; }
    IRepository<Entities.Polyclinic> Polyclinics { get; }
    IRepository<Entities.DoctorSchedule> DoctorSchedules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
