using HospitalNoShow.Domain.Enums;

namespace HospitalNoShow.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeOnly AppointmentTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    public bool IsFirstVisit { get; set; }
    public int SlotOrderInDay { get; set; } // The position of this appointment in the day's schedule
    public int TotalSlotsInDay { get; set; } // How busy that day is

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public NoShowAnalytics? NoShowAnalytics { get; set; }
}
