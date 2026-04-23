using HospitalNoShow.Domain.Enums;

namespace HospitalNoShow.Application.DTOs.Appointment;

public record AppointmentResponse(
    int Id,
    int PatientId,
    string PatientFullName,
    int DoctorId,
    string DoctorFullName,
    string DoctorTitle,
    string DoctorSpecialty,
    string PolyclinicName,
    DateTime AppointmentDate,
    TimeOnly AppointmentTime,
    AppointmentStatus Status,
    string? Notes,
    DateTime CreatedAt,
    NoShowRiskResponse? NoShowRisk
);

public record NoShowRiskResponse(
    double RiskScore,
    string RiskLevel,
    string WeatherCondition,
    string SmsResponse,
    bool IsReminderSent,
    DateTime CalculatedAt
);
