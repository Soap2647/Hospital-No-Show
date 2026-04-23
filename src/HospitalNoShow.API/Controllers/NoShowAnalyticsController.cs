using HospitalNoShow.Application.Interfaces;
using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalNoShow.API.Controllers;

/// <summary>
/// No-Show risk analizi ve SMS yönetimi.
/// </summary>
[Authorize(Roles = "Admin,Doctor")]
public class NoShowAnalyticsController(
    IUnitOfWork unitOfWork,
    INoShowRiskService riskService) : BaseApiController
{
    /// <summary>
    /// Belirli bir randevunun risk analizini getir.
    /// </summary>
    [HttpGet("appointment/{appointmentId:int}")]
    [ProducesResponseType(typeof(NoShowAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointment(int appointmentId, CancellationToken cancellationToken)
    {
        var analytics = await unitOfWork.NoShowAnalytics
            .GetByAppointmentIdAsync(appointmentId, cancellationToken);

        if (analytics is null) return NotFound(new { error = "Analiz verisi bulunamadı." });
        return Ok(analytics);
    }

    /// <summary>
    /// Yüksek riskli randevuları listele.
    /// </summary>
    [HttpGet("high-risk")]
    [ProducesResponseType(typeof(IReadOnlyList<NoShowAnalytics>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHighRisk(
        [FromQuery] double minRiskScore = 0.6,
        CancellationToken cancellationToken = default)
    {
        var analytics = await unitOfWork.NoShowAnalytics
            .GetHighRiskAppointmentsAsync(minRiskScore, cancellationToken);
        return Ok(analytics);
    }

    /// <summary>
    /// Randevunun risk skorunu yeniden hesapla.
    /// </summary>
    [HttpPost("appointment/{appointmentId:int}/recalculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Recalculate(int appointmentId, CancellationToken cancellationToken)
    {
        var analytics = await unitOfWork.NoShowAnalytics
            .GetByAppointmentIdAsync(appointmentId, cancellationToken);

        if (analytics is null) return NotFound(new { error = "Analiz kaydı bulunamadı." });

        var result = await riskService.RecalculateRiskAsync(appointmentId, cancellationToken);

        // Güncel skoru kaydet
        analytics.RiskScore = result.RiskScore;
        analytics.UpdatedAt = DateTime.UtcNow;
        unitOfWork.NoShowAnalytics.Update(analytics);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            appointmentId,
            result.RiskScore,
            result.RiskLevel,
            result.RiskExplanation,
            recalculatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// SMS cevabını kaydet ve riski güncelle.
    /// </summary>
    [HttpPatch("appointment/{appointmentId:int}/sms-response")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSmsResponse(
        int appointmentId,
        [FromBody] SmsResponseUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var analytics = await unitOfWork.NoShowAnalytics
            .GetByAppointmentIdAsync(appointmentId, cancellationToken);

        if (analytics is null) return NotFound();

        analytics.SmsResponse = request.Response;
        analytics.SmsRespondedAt = DateTime.UtcNow;
        analytics.UpdatedAt = DateTime.UtcNow;

        if (!analytics.IsReminderSent)
        {
            analytics.IsReminderSent = true;
            analytics.ReminderSentAt = DateTime.UtcNow;
        }

        unitOfWork.NoShowAnalytics.Update(analytics);

        // SMS cevabına göre riski yeniden hesapla
        var recalculated = await riskService.RecalculateRiskAsync(appointmentId, cancellationToken);
        analytics.SmsResponseWeight = recalculated.RiskScore - analytics.RiskScore;
        analytics.RiskScore = recalculated.RiskScore;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Hatırlatma SMS'i gönderilmesi gereken randevuları listele.
    /// </summary>
    [HttpGet("pending-reminders")]
    [ProducesResponseType(typeof(IReadOnlyList<NoShowAnalytics>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingReminders(CancellationToken cancellationToken)
    {
        var pending = await unitOfWork.NoShowAnalytics.GetPendingReminderSmsAsync(cancellationToken);
        return Ok(pending);
    }

    /// <summary>
    /// Doktora ait ortalama no-show risk skoru.
    /// </summary>
    [HttpGet("doctor/{doctorId:int}/average-risk")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorAverageRisk(int doctorId, CancellationToken cancellationToken)
    {
        var avg = await unitOfWork.NoShowAnalytics
            .GetAverageRiskScoreForDoctorAsync(doctorId, cancellationToken);
        return Ok(new { doctorId, averageRiskScore = avg, riskLevel = GetRiskLevel(avg) });
    }

    private static string GetRiskLevel(double score) => score switch
    {
        <= 0.30 => "Low",
        <= 0.60 => "Medium",
        <= 0.80 => "High",
        _ => "Critical"
    };
}

public record SmsResponseUpdateRequest(
    [System.ComponentModel.DataAnnotations.Required] SmsResponseType Response
);
