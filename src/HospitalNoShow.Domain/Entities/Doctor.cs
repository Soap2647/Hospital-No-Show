namespace HospitalNoShow.Domain.Entities;

public class Doctor
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty; // Prof. Dr., Doç. Dr., Dr.
    public string DiplomaCertificateNumber { get; set; } = string.Empty;
    public int PolyclinicId { get; set; }
    public int MaxDailyPatients { get; set; } = 20;
    public int AverageAppointmentDurationMinutes { get; set; } = 15;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Polyclinic Polyclinic { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<DoctorSchedule> Schedules { get; set; } = [];
}
