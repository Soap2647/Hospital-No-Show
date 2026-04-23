using System.ComponentModel.DataAnnotations;

namespace HospitalNoShow.BlazorClient.Models;

public enum AppointmentStatus
{
    Scheduled = 0,
    Completed = 1,
    NoShow = 2,
    Cancelled = 3,
    Rescheduled = 4
}

public record NoShowRiskInfo(
    double RiskScore,
    string RiskLevel,
    string WeatherCondition,
    string SmsResponse,
    bool IsReminderSent,
    DateTime CalculatedAt
);

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
    NoShowRiskInfo? NoShowRisk
)
{
    public string StatusDisplay => Status switch
    {
        AppointmentStatus.Scheduled => "Planlandı",
        AppointmentStatus.Completed => "Tamamlandı",
        AppointmentStatus.NoShow => "Gelmedi",
        AppointmentStatus.Cancelled => "İptal",
        AppointmentStatus.Rescheduled => "Ertelendi",
        _ => "Bilinmiyor"
    };

    public string RiskBadgeColor => NoShowRisk?.RiskLevel switch
    {
        "Low" => "success",
        "Medium" => "warning",
        "High" => "error",
        "Critical" => "error",
        _ => "default"
    };

    public bool IsHighRisk => (NoShowRisk?.RiskScore ?? 0) > 0.70;
}

public class CreateAppointmentModel
{
    [Required(ErrorMessage = "Doktor seçimi zorunludur.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir doktor seçin.")]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "Randevu tarihi zorunludur.")]
    public DateTime? AppointmentDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Randevu saati zorunludur.")]
    public TimeSpan? AppointmentTime { get; set; } = new TimeSpan(9, 0, 0);

    public string? Notes { get; set; }
}

public record UpdateStatusRequest(AppointmentStatus Status, string? Reason = null);
public record CancelRequest(string Reason);
