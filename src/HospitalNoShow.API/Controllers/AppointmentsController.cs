using System.Security.Claims;
using HospitalNoShow.Application.DTOs.Appointment;
using HospitalNoShow.Application.Interfaces;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalNoShow.API.Controllers;

/// <summary>
/// Randevu yönetimi ve no-show risk sorgulamaları.
/// </summary>
[Authorize]
public class AppointmentsController(
    IAppointmentService appointmentService,
    IUnitOfWork unitOfWork) : BaseApiController
{
    /// <summary>
    /// Admin: tüm randevuları listele (isteğe bağlı durum filtresi).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] AppointmentStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetAllForAdminAsync(status, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Admin: Dashboard için hafif istatistik özeti.
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var context = HttpContext.RequestServices
            .GetRequiredService<HospitalNoShow.Infrastructure.Data.ApplicationDbContext>();

        var today = DateTime.Today;

        var totalCount       = await context.Appointments.CountAsync(cancellationToken);
        var todayCount       = await context.Appointments.CountAsync(a => a.AppointmentDate.Date == today, cancellationToken);
        var completedCount   = await context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed, cancellationToken);
        var noShowCount      = await context.Appointments.CountAsync(a => a.Status == AppointmentStatus.NoShow, cancellationToken);
        var scheduledCount   = await context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Scheduled, cancellationToken);
        var cancelledCount   = await context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled, cancellationToken);
        var highRiskCount    = await context.NoShowAnalytics.CountAsync(a => a.RiskScore > 0.70, cancellationToken);

        // Poliklinik bazlı no-show oranı
        var byPoly = await context.Appointments
            .GroupBy(a => a.Doctor.Polyclinic.Name)
            .Select(g => new
            {
                Name = g.Key,
                Total = g.Count(),
                NoShow = g.Count(a => a.Status == AppointmentStatus.NoShow)
            })
            .OrderByDescending(x => x.NoShow)
            .Take(6)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            totalCount, todayCount, completedCount, noShowCount, scheduledCount, cancelledCount, highRiskCount,
            noShowRate = totalCount > 0 ? (double)noShowCount / totalCount * 100 : 0,
            polyclinicStats = byPoly.Select(x => new
            {
                name = x.Name?.Length > 12 ? x.Name[..12] + "…" : x.Name,
                noShowRate = x.Total > 0 ? (double)x.NoShow / x.Total * 100 : 0
            })
        });
    }

    /// <summary>
    /// Yeni randevu oluştur. Risk skoru otomatik hesaplanır.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Patient,Admin")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Hasta kendi ID'sini token'dan alır
        var patient = await unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
        if (patient is null) return Forbid();

        var result = await appointmentService.CreateAsync(patient.Id, request, cancellationToken);
        return ToCreatedResult(result, nameof(GetById), new { id = result.Value?.Id });
    }

    /// <summary>
    /// Randevu detayını getir.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Hastanın kendi randevularını listele.
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAppointments(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var patient = await unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
        if (patient is null) return Forbid();

        var result = await appointmentService.GetByPatientAsync(patient.Id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Belirli bir hastanın randevularını getir (Admin/Doktor yetkisi).
    /// </summary>
    [HttpGet("patient/{patientId:int}")]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatient(int patientId, CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetByPatientAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Doktorun günlük randevu listesi.
    /// </summary>
    [HttpGet("doctor/{doctorId:int}/date/{date}")]
    [Authorize(Roles = "Admin,Doctor,Patient")] // Hasta da müsait saatleri görebilsin
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDoctorAndDate(
        int doctorId,
        [FromRoute] string date,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest(new { error = "Geçersiz tarih formatı. YYYY-MM-DD kullanın." });

        var result = await appointmentService.GetByDoctorAndDateAsync(doctorId, parsedDate, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Randevu durumunu güncelle (Admin ve Doktor).
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.UpdateStatusAsync(
            id, request.Status, request.Reason, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Randevuyu iptal et.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.CancelAsync(id, request.Reason, cancellationToken);
        return ToActionResult(result);
    }
}

public record UpdateStatusRequest(
    [System.ComponentModel.DataAnnotations.Required] AppointmentStatus Status,
    string? Reason = null
);

public record CancelRequest(
    [System.ComponentModel.DataAnnotations.Required,
     System.ComponentModel.DataAnnotations.MaxLength(500)] string Reason
);
