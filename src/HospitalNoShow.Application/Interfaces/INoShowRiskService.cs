using HospitalNoShow.Application.DTOs.NoShow;
using HospitalNoShow.Domain.Entities;

namespace HospitalNoShow.Application.Interfaces;

public interface INoShowRiskService
{
    /// <summary>
    /// Yeni oluşturulan bir randevu için gelmeme riski hesaplar (0.0 - 1.0 arası).
    /// </summary>
    Task<NoShowRiskCalculationResult> CalculateRiskAsync(
        Appointment appointment,
        Patient patient,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mevcut bir randevunun riskini yeniden hesaplar (SMS cevabı, hava durumu gibi güncel verilerle).
    /// </summary>
    Task<NoShowRiskCalculationResult> RecalculateRiskAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);
}
