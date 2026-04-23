using System.ComponentModel.DataAnnotations;

namespace HospitalNoShow.Application.DTOs.Appointment;

public record CreateAppointmentRequest(
    [Required] int DoctorId,
    [Required] DateOnly AppointmentDate,
    [Required] TimeOnly AppointmentTime,
    string? Notes = null
);
