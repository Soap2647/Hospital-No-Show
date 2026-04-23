using HospitalNoShow.Domain.Enums;

namespace HospitalNoShow.Domain.Entities;

public class Patient
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty; // TC Kimlik No
    public DateOnly DateOfBirth { get; set; }
    public int Age => CalculateAge();
    public Gender Gender { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double DistanceToHospitalKm { get; set; }
    public InsuranceType InsuranceType { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public bool HasChronicDisease { get; set; }
    public string? ChronicDiseaseNotes { get; set; }
    public int TotalAppointments { get; set; }
    public int NoShowCount { get; set; }
    public double NoShowRate => TotalAppointments > 0
        ? Math.Round((double)NoShowCount / TotalAppointments, 4)
        : 0;

    // Additional Health Metrics
    public int? HeightCm { get; set; }
    public double? WeightKg { get; set; }
    public string? BloodType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<MedicalHistory> MedicalHistories { get; set; } = [];

    private int CalculateAge()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth > today.AddYears(-age)) age--;
        return age;
    }
}
