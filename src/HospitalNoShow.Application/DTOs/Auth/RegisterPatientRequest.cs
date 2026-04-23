using System.ComponentModel.DataAnnotations;
using HospitalNoShow.Application.Validations;
using HospitalNoShow.Domain.Enums;

namespace HospitalNoShow.Application.DTOs.Auth;

public record RegisterPatientRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, TcKimlik] string IdentityNumber,
    [Required] DateOnly DateOfBirth,
    [Required] Gender Gender,
    [Required, RegularExpression(@"^5\d{9}$", ErrorMessage = "Telefon numarası 5 ile başlamalı ve boşluksuz 10 haneli olmalıdır (Örn: 5551234567).")] string PhoneNumber,
    [Required] string Address,
    [Required] string City,
    [Required, Range(0, 500)] double DistanceToHospitalKm,
    InsuranceType InsuranceType = InsuranceType.None,
    string? InsurancePolicyNumber = null,
    bool HasChronicDisease = false,
    string? ChronicDiseaseNotes = null,
    [Range(50, 250)] int? HeightCm = null,
    [Range(10, 300)] double? WeightKg = null,
    string? BloodType = null
);
