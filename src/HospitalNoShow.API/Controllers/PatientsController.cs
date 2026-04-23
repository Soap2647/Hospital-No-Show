using System.Security.Claims;
using HospitalNoShow.Application.DTOs.Patient;
using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalNoShow.API.Controllers;

/// <summary>
/// Hasta yönetimi.
/// </summary>
[Authorize]
public class PatientsController(IUnitOfWork unitOfWork) : BaseApiController
{
    /// <summary>
    /// Tüm hastaları listele (Admin).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var patients = await unitOfWork.Patients.GetAllAsync(cancellationToken);
        return Ok(patients.Select(MapToResponse));
    }

    /// <summary>
    /// Hastanın kendi profilini görüntüle.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var patient = await unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
        if (patient is null) return NotFound();

        var full = await unitOfWork.Patients.GetFullProfileAsync(patient.Id, cancellationToken);
        return full is null ? NotFound() : Ok(MapToResponse(full));
    }

    /// <summary>
    /// Hasta detayını getir (Admin/Doktor).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetFullProfileAsync(id, cancellationToken);
        if (patient is null) return NotFound(new { error = "Hasta bulunamadı." });
        return Ok(MapToResponse(patient));
    }

    /// <summary>
    /// Yüksek no-show riskli hastaları listele (Admin/Doktor).
    /// </summary>
    [HttpGet("high-risk")]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHighRisk(
        [FromQuery] double minNoShowRate = 0.3,
        CancellationToken cancellationToken = default)
    {
        var patients = await unitOfWork.Patients
            .GetHighRiskPatientsAsync(minNoShowRate, cancellationToken);
        return Ok(patients.Select(MapToResponse));
    }

    private static PatientResponse MapToResponse(Patient p) => new(
        Id: p.Id,
        UserId: p.UserId,
        FullName: p.User?.FullName ?? string.Empty,
        Email: p.User?.Email ?? string.Empty,
        PhoneNumber: p.PhoneNumber,
        Age: p.Age,
        Gender: p.Gender,
        City: p.City,
        DistanceToHospitalKm: p.DistanceToHospitalKm,
        InsuranceType: p.InsuranceType,
        HasChronicDisease: p.HasChronicDisease,
        ChronicDiseaseNotes: p.ChronicDiseaseNotes,
        HeightCm: p.HeightCm,
        WeightKg: p.WeightKg,
        BloodType: p.BloodType,
        TotalAppointments: p.TotalAppointments,
        NoShowCount: p.NoShowCount,
        NoShowRate: p.NoShowRate,
        CreatedAt: p.CreatedAt
    );
}
