using HospitalNoShow.Application.DTOs.Doctor;
using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalNoShow.API.Controllers;

/// <summary>
/// Doktor yönetimi.
/// </summary>
[Authorize]
public class DoctorsController(IUnitOfWork unitOfWork) : BaseApiController
{
    /// <summary>
    /// Tüm doktorları listele.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var doctors = await unitOfWork.Doctors.GetAllAsync(cancellationToken);
        return Ok(doctors.Select(MapToResponse));
    }

    /// <summary>
    /// Uzmanlık alanına göre filtrele.
    /// </summary>
    [HttpGet("by-specialty")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySpecialty(
        [FromQuery] string specialty,
        CancellationToken cancellationToken)
    {
        var doctors = await unitOfWork.Doctors.GetBySpecialtyAsync(specialty, cancellationToken);
        return Ok(doctors.Select(MapToResponse));
    }

    /// <summary>
    /// Doktor detayını ve çalışma saatlerini getir.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var doctor = await unitOfWork.Doctors.GetWithSchedulesAsync(id, cancellationToken);
        if (doctor is null) return NotFound(new { error = "Doktor bulunamadı." });
        return Ok(MapToResponse(doctor));
    }

    /// <summary>
    /// Kullanıcı ID'sine göre doktor profili getir.
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        var doctor = await unitOfWork.Doctors.GetByUserIdAsync(userId, cancellationToken);
        if (doctor is null) return NotFound(new { error = "Bu kullanıcıya ait doktor profili bulunamadı." });
        return Ok(MapToResponse(doctor));
    }

    /// <summary>
    /// Doktorun belirtilen tarihteki müsaitliğini kontrol et.
    /// </summary>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckAvailability(
        int id,
        [FromQuery] DateTime date,
        [FromQuery] string time,
        CancellationToken cancellationToken)
    {
        if (!TimeOnly.TryParse(time, out var parsedTime))
            return BadRequest(new { error = "Geçersiz saat formatı. HH:mm kullanın." });

        var isAvailable = await unitOfWork.Doctors.IsAvailableAtAsync(id, date, parsedTime, cancellationToken);
        return Ok(new { isAvailable, date, time });
    }

    private static DoctorResponse MapToResponse(Doctor d) => new(
        Id: d.Id,
        UserId: d.UserId,
        FullName: d.User?.FullName ?? string.Empty,
        Email: d.User?.Email ?? string.Empty,
        Title: d.Title,
        Specialty: d.Specialty,
        PolyclinicName: d.Polyclinic?.Name ?? string.Empty,
        Department: d.Polyclinic?.Department ?? string.Empty,
        MaxDailyPatients: d.MaxDailyPatients,
        AverageAppointmentDurationMinutes: d.AverageAppointmentDurationMinutes
    );
}
