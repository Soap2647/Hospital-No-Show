using HospitalNoShow.Application.Common;
using HospitalNoShow.Application.DTOs.Appointment;
using HospitalNoShow.Domain.Enums;

namespace HospitalNoShow.Application.Interfaces;

public interface IAppointmentService
{
    Task<Result<AppointmentResponse>> CreateAsync(
        int patientId,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AppointmentResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AppointmentResponse>>> GetByPatientAsync(
        int patientId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AppointmentResponse>>> GetByDoctorAsync(
        int doctorId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AppointmentResponse>>> GetByDoctorAndDateAsync(
        int doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateStatusAsync(
        int appointmentId,
        AppointmentStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<Result> CancelAsync(
        int appointmentId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AppointmentResponse>>> GetAllForAdminAsync(
        AppointmentStatus? status = null,
        CancellationToken cancellationToken = default);
}
