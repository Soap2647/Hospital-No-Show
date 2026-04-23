namespace HospitalNoShow.Domain.Entities;

public class Medication
{
    public int Id { get; set; }
    public int MedicalHistoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty; // Günde 2 kez, 8 saatte bir...
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsOngoing => EndDate is null;
    public string? Notes { get; set; }

    // Navigation properties
    public MedicalHistory MedicalHistory { get; set; } = null!;
}
