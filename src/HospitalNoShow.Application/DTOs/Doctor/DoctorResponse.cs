namespace HospitalNoShow.Application.DTOs.Doctor;

public record DoctorResponse(
    int Id,
    string UserId,
    string FullName,
    string Email,
    string Title,
    string Specialty,
    string PolyclinicName,
    string Department,
    int MaxDailyPatients,
    int AverageAppointmentDurationMinutes
);
