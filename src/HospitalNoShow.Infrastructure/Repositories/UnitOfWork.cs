using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Interfaces;
using HospitalNoShow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace HospitalNoShow.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy initialization with field-backed properties
    private IPatientRepository? _patients;
    private IDoctorRepository? _doctors;
    private IAppointmentRepository? _appointments;
    private INoShowAnalyticsRepository? _noShowAnalytics;
    private IRepository<MedicalHistory>? _medicalHistories;
    private IRepository<Medication>? _medications;
    private IRepository<Polyclinic>? _polyclinics;
    private IRepository<DoctorSchedule>? _doctorSchedules;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IPatientRepository Patients
        => _patients ??= new PatientRepository(_context);

    public IDoctorRepository Doctors
        => _doctors ??= new DoctorRepository(_context);

    public IAppointmentRepository Appointments
        => _appointments ??= new AppointmentRepository(_context);

    public INoShowAnalyticsRepository NoShowAnalytics
        => _noShowAnalytics ??= new NoShowAnalyticsRepository(_context);

    public IRepository<MedicalHistory> MedicalHistories
        => _medicalHistories ??= new BaseRepository<MedicalHistory>(_context);

    public IRepository<Medication> Medications
        => _medications ??= new BaseRepository<Medication>(_context);

    public IRepository<Polyclinic> Polyclinics
        => _polyclinics ??= new BaseRepository<Polyclinic>(_context);

    public IRepository<DoctorSchedule> DoctorSchedules
        => _doctorSchedules ??= new BaseRepository<DoctorSchedule>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) throw new InvalidOperationException("Transaction not started.");
        await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) throw new InvalidOperationException("Transaction not started.");
        await _transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
