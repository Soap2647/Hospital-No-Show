using HospitalNoShow.Domain.Enums;

namespace HospitalNoShow.Application.DTOs.Patient;

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
);
