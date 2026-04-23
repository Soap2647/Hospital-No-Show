using HospitalNoShow.Application.DTOs.NoShow;
using HospitalNoShow.Application.Interfaces;
using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Enums;
using HospitalNoShow.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalNoShow.Application.Services;

/// <summary>
/// Ağırlıklı faktörler kullanarak hastanın randevuya gelmeme riskini hesaplar.
///
/// Algoritma mantığı:
/// - Her faktör 0.0 - 1.0 arası normalize edilir
/// - Her faktörün sabit bir ağırlığı (weight) vardır, toplamları 1.0 = 100%
/// - Risk skoru = SUM(faktör_değeri × faktör_ağırlığı)
/// - Sonuç 0.0 (kesinlikle gelir) - 1.0 (kesinlikle gelmez) arasında
/// </summary>
public sealed class NoShowRiskService(
    IUnitOfWork unitOfWork,
    ILogger<NoShowRiskService> logger) : INoShowRiskService
{
    // ── Faktör Ağırlıkları (toplam = 1.0) ──────────────────────────────────
    private const double WeightPreviousNoShow    = 0.35;  // En güçlü sinyal
    private const double WeightAgeGroup          = 0.10;
    private const double WeightDistance          = 0.12;
    private const double WeightAppointmentTime   = 0.10;
    private const double WeightAppointmentDay    = 0.08;
    private const double WeightInsuranceType     = 0.08;
    private const double WeightFirstVisit        = 0.07;
    private const double WeightSlotBusyness      = 0.10;  // Günün doluluk oranı
    // ── Dinamik Faktörler (SMS, Hava) ayrıca eklenir ─────────────────────
    // Statik faktörlerin toplam ağırlığı = 1.0; dinamik faktörler ile
    // birlikte skoru normalize ediyoruz.

    public async Task<NoShowRiskCalculationResult> CalculateRiskAsync(
        Appointment appointment,
        Patient patient,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Calculating no-show risk for AppointmentId={AppointmentId}, PatientId={PatientId}",
            appointment.Id, patient.Id);

        // 1. Geçmiş gelmeme oranı (en güçlü tahmin edici)
        var previousNoShowFactor = CalculatePreviousNoShowFactor(patient);

        // 2. Yaş grubu riski
        var ageGroupFactor = CalculateAgeGroupFactor(patient.Age);

        // 3. Hastaneye uzaklık
        var distanceFactor = CalculateDistanceFactor(patient.DistanceToHospitalKm);

        // 4. Randevu saati (çok erken veya akşam saatleri riskli)
        var timeFactor = CalculateTimeFactor(appointment.AppointmentTime);

        // 5. Haftanın günü (Pazartesi ve Cuma riskli)
        var dayFactor = CalculateDayFactor(appointment.AppointmentDate.DayOfWeek);

        // 6. Sigorta tipi
        var insuranceFactor = CalculateInsuranceFactor(patient.InsuranceType);

        // 7. İlk ziyaret mi? (ilk ziyarette gelmeme oranı yüksek)
        var firstVisitFactor = appointment.IsFirstVisit ? 0.6 : 0.2;

        // 8. Günün doluluk oranı (çok yoğun günlerde hasta çekiniyor)
        var busynessFactor = CalculateBusynessFactor(
            appointment.SlotOrderInDay,
            appointment.TotalSlotsInDay);

        // ── Ağırlıklı ortalama ──────────────────────────────────────────────
        var rawScore =
            (previousNoShowFactor * WeightPreviousNoShow) +
            (ageGroupFactor       * WeightAgeGroup)       +
            (distanceFactor       * WeightDistance)       +
            (timeFactor           * WeightAppointmentTime)+
            (dayFactor            * WeightAppointmentDay) +
            (insuranceFactor      * WeightInsuranceType)  +
            (firstVisitFactor     * WeightFirstVisit)     +
            (busynessFactor       * WeightSlotBusyness);

        // Skoru [0, 1] arasında sınırla
        var finalScore = Math.Clamp(Math.Round(rawScore, 4), 0.0, 1.0);

        var riskLevel = GetRiskLevel(finalScore);
        var explanation = BuildExplanation(
            finalScore, previousNoShowFactor, ageGroupFactor,
            distanceFactor, timeFactor, dayFactor, patient);

        logger.LogInformation(
            "Risk calculated: Score={Score}, Level={Level}, PatientId={PatientId}",
            finalScore, riskLevel, patient.Id);

        return new NoShowRiskCalculationResult(
            RiskScore: finalScore,
            RiskLevel: riskLevel,
            PreviousNoShowRateWeight: Math.Round(previousNoShowFactor * WeightPreviousNoShow, 4),
            AgeGroupWeight: Math.Round(ageGroupFactor * WeightAgeGroup, 4),
            DistanceWeight: Math.Round(distanceFactor * WeightDistance, 4),
            AppointmentTimeWeight: Math.Round(timeFactor * WeightAppointmentTime, 4),
            AppointmentDayWeight: Math.Round(dayFactor * WeightAppointmentDay, 4),
            InsuranceTypeWeight: Math.Round(insuranceFactor * WeightInsuranceType, 4),
            FirstVisitWeight: Math.Round(firstVisitFactor * WeightFirstVisit, 4),
            RiskExplanation: explanation
        );
    }

    public async Task<NoShowRiskCalculationResult> RecalculateRiskAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await unitOfWork.Appointments.GetWithDetailsAsync(appointmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Appointment {appointmentId} not found.");

        var analytics = await unitOfWork.NoShowAnalytics
            .GetByAppointmentIdAsync(appointmentId, cancellationToken);

        var baseResult = await CalculateRiskAsync(appointment, appointment.Patient, cancellationToken);

        // SMS cevabı varsa skoru güncelle
        if (analytics is not null)
        {
            var adjustedScore = ApplySmsResponseAdjustment(baseResult.RiskScore, analytics.SmsResponse);
            var adjustedLevel = GetRiskLevel(adjustedScore);

            return baseResult with
            {
                RiskScore = adjustedScore,
                RiskLevel = adjustedLevel
            };
        }

        return baseResult;
    }

    // ── Faktör Hesaplama Metodları ─────────────────────────────────────────

    /// <summary>
    /// Hasta daha önce hiç gelmemişse 0.0, %100 gelmemişse 1.0 döner.
    /// Yeterli veri yoksa (az randevu) güvenilirlik düşürülür.
    /// </summary>
    private static double CalculatePreviousNoShowFactor(Patient patient)
    {
        if (patient.TotalAppointments == 0) return 0.3; // Bilinmiyor, orta risk

        var rawRate = (double)patient.NoShowCount / patient.TotalAppointments;

        // Güven aralığı düzeltmesi: Az veriyle aşırı tahmin önlenir
        // Bayesian düzeltme: (noShows + prior) / (total + prior*2)
        const double prior = 3.0; // 3 randevuluk geçmiş varsayımı
        return (patient.NoShowCount + prior * 0.3) / (patient.TotalAppointments + prior);
    }

    /// <summary>
    /// Yaş grubuna göre gelmeme riski:
    /// - 18-30: orta yüksek (meşguliyet)
    /// - 31-50: düşük (sorumluluk)
    /// - 51-65: orta (ulaşım güçlüğü başlar)
    /// - 65+:   yüksek (mobilite kısıtlılığı)
    /// - 0-17:  refakatçıya bağlı, orta
    /// </summary>
    private static double CalculateAgeGroupFactor(int age) => age switch
    {
        < 18 => 0.35,
        >= 18 and < 30 => 0.50,
        >= 30 and < 50 => 0.20,
        >= 50 and < 65 => 0.35,
        _ => 0.55  // 65+
    };

    /// <summary>
    /// Mesafe faktörü: 0-5 km çok düşük, 50+ km çok yüksek.
    /// Sigmoid benzeri bir eğri kullanılır.
    /// </summary>
    private static double CalculateDistanceFactor(double distanceKm)
    {
        // 50 km'de 0.7 risk verecek şekilde ayarlanmış sigmoid
        const double k = 0.06;
        const double midpoint = 20.0;
        return 1.0 / (1.0 + Math.Exp(-k * (distanceKm - midpoint)));
    }

    /// <summary>
    /// Randevu saati riski:
    /// - 07:00-09:00: çok erken = yüksek risk
    /// - 09:00-12:00: sabah = düşük risk
    /// - 12:00-14:00: öğle arası = orta risk
    /// - 14:00-17:00: öğleden sonra = düşük-orta
    /// - 17:00+: akşam = orta-yüksek
    /// </summary>
    private static double CalculateTimeFactor(TimeOnly time)
    {
        var hour = time.Hour;
        return hour switch
        {
            < 7 => 0.70,
            7 or 8 => 0.60,      // Çok erken
            9 or 10 or 11 => 0.20, // İdeal sabah saatleri
            12 or 13 => 0.40,    // Öğle arası
            14 or 15 or 16 => 0.30, // İyi öğleden sonra
            17 or 18 => 0.55,    // Akşam başlangıcı
            _ => 0.65            // Geç akşam
        };
    }

    /// <summary>
    /// Haftanın günü riski:
    /// Pazartesi: hafta başı morali, Cuma: hafta sonu alışkanlığı
    /// </summary>
    private static double CalculateDayFactor(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => 0.45,
        DayOfWeek.Tuesday => 0.25,
        DayOfWeek.Wednesday => 0.20,
        DayOfWeek.Thursday => 0.25,
        DayOfWeek.Friday => 0.50,
        DayOfWeek.Saturday => 0.60,
        DayOfWeek.Sunday => 0.70,
        _ => 0.30
    };

    /// <summary>
    /// Sigorta türü riski:
    /// SGK hastası: düşük maliyet, davranışsal farklılık
    /// Özel sigorta: daha yüksek sorumluluk
    /// </summary>
    private static double CalculateInsuranceFactor(InsuranceType insurance) => insurance switch
    {
        InsuranceType.PrivateInsurance => 0.15,
        InsuranceType.SGK => 0.40,
        InsuranceType.GreenCard => 0.50,
        InsuranceType.SelfPay => 0.25,
        InsuranceType.None => 0.55,
        _ => 0.35
    };

    /// <summary>
    /// Günün doluluk oranı: Son randevular genellikle daha yüksek riskli.
    /// Güne ait randevunun sıra numarasının toplam randevuya oranı.
    /// </summary>
    private static double CalculateBusynessFactor(int slotOrder, int totalSlots)
    {
        if (totalSlots == 0) return 0.3;
        var ratio = (double)slotOrder / totalSlots;
        // Son %20 randevu yüksek risk
        return ratio > 0.8 ? 0.60 : ratio > 0.5 ? 0.35 : 0.20;
    }

    /// <summary>
    /// SMS cevabına göre skoru düzeltir.
    /// "Onayladım" → risk düşer, "İptal" → risk artar.
    /// </summary>
    private static double ApplySmsResponseAdjustment(double currentScore, SmsResponseType smsResponse)
    {
        var adjustment = smsResponse switch
        {
            SmsResponseType.Confirmed => -0.25,   // Onayladı, risk azalır
            SmsResponseType.Cancelled => +0.40,   // İptal dedi, neredeyse kesin gelmez
            SmsResponseType.NoResponse => +0.10,  // Cevap yok, hafif artar
            SmsResponseType.Sent => 0.0,           // Henüz cevap bekleniyor
            _ => 0.0
        };

        return Math.Clamp(currentScore + adjustment, 0.0, 1.0);
    }

    private static string GetRiskLevel(double score) => score switch
    {
        <= 0.30 => "Low",
        <= 0.60 => "Medium",
        <= 0.80 => "High",
        _ => "Critical"
    };

    private static string BuildExplanation(
        double finalScore,
        double noShowFactor,
        double ageFactor,
        double distanceFactor,
        double timeFactor,
        double dayFactor,
        Patient patient)
    {
        var factors = new List<string>();

        if (noShowFactor > 0.5)
            factors.Add($"yüksek geçmiş gelmeme oranı (%{patient.NoShowRate * 100:F0})");
        if (ageFactor > 0.4)
            factors.Add($"yaş grubu riski ({patient.Age} yaş)");
        if (distanceFactor > 0.5)
            factors.Add($"hastaneden uzaklık ({patient.DistanceToHospitalKm:F1} km)");
        if (timeFactor > 0.5)
            factors.Add("riskli randevu saati");
        if (dayFactor > 0.4)
            factors.Add("riskli haftanın günü");

        if (!factors.Any())
            return $"Risk skoru {finalScore:F2} - Belirgin risk faktörü bulunmuyor.";

        return $"Risk skoru {finalScore:F2} - Başlıca faktörler: {string.Join(", ", factors)}.";
    }
}
