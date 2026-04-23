using HospitalNoShow.Application.Common;
using HospitalNoShow.Application.DTOs.Appointment;
using HospitalNoShow.Application.Interfaces;
using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalNoShow.Application.Services;

public sealed class AppointmentService(
    IUnitOfWork unitOfWork,
    INoShowRiskService riskService,
    ILogger<AppointmentService> logger) : IAppointmentService
{
    public async Task<Result<AppointmentResponse>> CreateAsync(
        int patientId,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await unitOfWork.Patients.GetFullProfileAsync(patientId, cancellationToken);
        if (patient is null)
            return Result<AppointmentResponse>.Failure("Hasta bulunamadı.");

        var doctor = await unitOfWork.Doctors.GetWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<AppointmentResponse>.Failure("Doktor bulunamadı.");

        var appointmentDateTime = request.AppointmentDate.ToDateTime(request.AppointmentTime);

        // Tarih geçmişte mi? (UTC yerine yerel tarih karşılaştırması - Türkiye UTC+3)
        if (request.AppointmentDate < DateOnly.FromDateTime(DateTime.Now))
            return Result<AppointmentResponse>.Failure("Randevu tarihi gelecekte olmalıdır.");

        // Doktor müsait mi?
        var isAvailable = await unitOfWork.Doctors.IsAvailableAtAsync(
            request.DoctorId,
            appointmentDateTime,
            request.AppointmentTime,
            cancellationToken);

        if (!isAvailable)
            return Result<AppointmentResponse>.Failure("Seçilen saat için doktor müsait değil.");

        var slotOrder = await unitOfWork.Appointments.GetSlotOrderInDayAsync(
            request.DoctorId,
            request.AppointmentDate,
            request.AppointmentTime,
            cancellationToken);

        var totalSlots = await unitOfWork.Appointments.GetTotalSlotsForDayAsync(
            request.DoctorId,
            request.AppointmentDate,
            cancellationToken);

        var isFirstVisit = !await unitOfWork.Appointments.AnyAsync(
            a => a.PatientId == patientId && a.DoctorId == request.DoctorId,
            cancellationToken);

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = request.DoctorId,
            AppointmentDate = appointmentDateTime,
            AppointmentTime = request.AppointmentTime,
            Status = AppointmentStatus.Scheduled,
            Notes = request.Notes,
            IsFirstVisit = isFirstVisit,
            SlotOrderInDay = slotOrder,
            TotalSlotsInDay = totalSlots + 1 // Bu randevu da eklenince
        };

        try
        {
            await unitOfWork.Appointments.AddAsync(appointment, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Risk hesapla ve kaydet
            var riskResult = await riskService.CalculateRiskAsync(appointment, patient, cancellationToken);

            var analytics = new NoShowAnalytics
            {
                AppointmentId = appointment.Id,
                RiskScore = riskResult.RiskScore,
                PreviousNoShowRateWeight = riskResult.PreviousNoShowRateWeight,
                AgeGroupWeight = riskResult.AgeGroupWeight,
                DistanceWeight = riskResult.DistanceWeight,
                AppointmentTimeWeight = riskResult.AppointmentTimeWeight,
                AppointmentDayWeight = riskResult.AppointmentDayWeight,
                InsuranceTypeWeight = riskResult.InsuranceTypeWeight,
                FirstVisitWeight = riskResult.FirstVisitWeight
            };

            await unitOfWork.NoShowAnalytics.AddAsync(analytics, cancellationToken);
            await unitOfWork.Patients.UpdateNoShowStatsAsync(patientId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Appointment created: Id={Id}, PatientId={PatientId}, RiskScore={Risk}",
                appointment.Id, patientId, riskResult.RiskScore);

            var created = await unitOfWork.Appointments.GetWithDetailsAsync(appointment.Id, cancellationToken);
            return Result<AppointmentResponse>.Success(MapToResponse(created!));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating appointment for PatientId={PatientId}", patientId);
            return Result<AppointmentResponse>.Failure("Randevu oluşturulurken bir hata oluştu.");
        }
    }

    public async Task<Result<AppointmentResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var appointment = await unitOfWork.Appointments.GetWithDetailsAsync(id, cancellationToken);
        if (appointment is null)
            return Result<AppointmentResponse>.Failure("Randevu bulunamadı.");

        return Result<AppointmentResponse>.Success(MapToResponse(appointment));
    }

    public async Task<Result<IReadOnlyList<AppointmentResponse>>> GetByPatientAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        var appointments = await unitOfWork.Appointments.GetByPatientAsync(patientId, cancellationToken);
        return Result<IReadOnlyList<AppointmentResponse>>.Success(
            appointments.Select(MapToResponse).ToList());
    }

    public async Task<Result<IReadOnlyList<AppointmentResponse>>> GetByDoctorAsync(
        int doctorId,
        CancellationToken cancellationToken = default)
    {
        var appointments = await unitOfWork.Appointments.GetByDoctorAsync(doctorId, cancellationToken);
        return Result<IReadOnlyList<AppointmentResponse>>.Success(
            appointments.Select(MapToResponse).ToList());
    }

    public async Task<Result<IReadOnlyList<AppointmentResponse>>> GetByDoctorAndDateAsync(
        int doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var appointments = await unitOfWork.Appointments
            .GetByDoctorAndDateAsync(doctorId, date, cancellationToken);
        return Result<IReadOnlyList<AppointmentResponse>>.Success(
            appointments.Select(MapToResponse).ToList());
    }

    public async Task<Result> UpdateStatusAsync(
        int appointmentId,
        AppointmentStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var appointment = await unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
            return Result.Failure("Randevu bulunamadı.");

        appointment.Status = newStatus;
        if (reason is not null) appointment.CancellationReason = reason;

        unitOfWork.Appointments.Update(appointment);

        // No-Show kaydedilince hastanın istatistiklerini güncelle
        if (newStatus is AppointmentStatus.NoShow or AppointmentStatus.Completed)
            await unitOfWork.Patients.UpdateNoShowStatsAsync(appointment.PatientId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AppointmentResponse>>> GetAllForAdminAsync(
        AppointmentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var appointments = await unitOfWork.Appointments.GetAllForAdminAsync(status, 10000, cancellationToken);
        return Result<IReadOnlyList<AppointmentResponse>>.Success(
            appointments.Select(MapToResponse).ToList());
    }

    public async Task<Result> CancelAsync(
        int appointmentId,
        string reason,
        CancellationToken cancellationToken = default)
        => await UpdateStatusAsync(appointmentId, AppointmentStatus.Cancelled, reason, cancellationToken);

    private static AppointmentResponse MapToResponse(Appointment a) => new(
        Id: a.Id,
        PatientId: a.PatientId,
        PatientFullName: a.Patient.User.FullName,
        DoctorId: a.DoctorId,
        DoctorFullName: a.Doctor.User.FullName,
        DoctorTitle: a.Doctor.Title,
        DoctorSpecialty: a.Doctor.Specialty,
        PolyclinicName: a.Doctor.Polyclinic.Name,
        AppointmentDate: a.AppointmentDate,
        AppointmentTime: a.AppointmentTime,
        Status: a.Status,
        Notes: a.Notes,
        CreatedAt: a.CreatedAt,
        NoShowRisk: a.NoShowAnalytics is null ? null : new NoShowRiskResponse(
            RiskScore: a.NoShowAnalytics.RiskScore,
            RiskLevel: a.NoShowAnalytics.RiskLevel,
            WeatherCondition: a.NoShowAnalytics.WeatherCondition.ToString(),
            SmsResponse: a.NoShowAnalytics.SmsResponse.ToString(),
            IsReminderSent: a.NoShowAnalytics.IsReminderSent,
            CalculatedAt: a.NoShowAnalytics.CalculatedAt
        )
    );
}
