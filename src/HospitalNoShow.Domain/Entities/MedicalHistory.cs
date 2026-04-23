namespace HospitalNoShow.Domain.Entities;

public class MedicalHistory
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string DiagnosisCode { get; set; } = string.Empty; // ICD-10 kodu
    public string DiagnosisName { get; set; } = string.Empty;
    public string? DiagnosisDescription { get; set; }
    public DateOnly DiagnosisDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public ICollection<Medication> Medications { get; set; } = [];
}
