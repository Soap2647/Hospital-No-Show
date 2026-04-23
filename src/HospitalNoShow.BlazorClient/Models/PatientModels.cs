namespace HospitalNoShow.BlazorClient.Models;

public enum Gender { Male, Female, Other, PreferNotToSay }
public enum InsuranceType { None, SGK, PrivateInsurance, GreenCard, SelfPay }

public record PatientResponse(
    int Id,
    string UserId,
    string FullName,
    string Email,
    string PhoneNumber,
    int Age,
    Gender Gender,
    string City,
    double DistanceToHospitalKm,
    InsuranceType InsuranceType,
    bool HasChronicDisease,
    string? ChronicDiseaseNotes,
    int? HeightCm,
    double? WeightKg,
    string? BloodType,
    int TotalAppointments,
    int NoShowCount,
    double NoShowRate,
    DateTime CreatedAt
)
{
    public string InsuranceDisplay => InsuranceType switch
    {
        InsuranceType.SGK => "SGK",
        InsuranceType.PrivateInsurance => "Özel Sigorta",
        InsuranceType.GreenCard => "Yeşil Kart",
        InsuranceType.SelfPay => "Ücretli",
        InsuranceType.None => "Sigortasız",
        _ => "Bilinmiyor"
    };

    public string GenderDisplay => Gender switch
    {
        Gender.Male => "Erkek",
        Gender.Female => "Kadın",
        Gender.Other => "Diğer",
        _ => "Belirtilmedi"
    };

    public string NoShowRateFormatted => $"{NoShowRate * 100:F0}%";
}
