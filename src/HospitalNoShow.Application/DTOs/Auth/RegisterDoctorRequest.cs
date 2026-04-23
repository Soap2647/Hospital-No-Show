using System.ComponentModel.DataAnnotations;

namespace HospitalNoShow.Application.DTOs.Auth;

public record RegisterDoctorRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, MaxLength(100)] string Specialty,
    [Required, MaxLength(50)] string Title,
    [Required, MaxLength(50)] string DiplomaCertificateNumber,
    [Required] int PolyclinicId,
    [Range(1, 100)] int MaxDailyPatients = 20,
    [Range(5, 120)] int AverageAppointmentDurationMinutes = 15
);
