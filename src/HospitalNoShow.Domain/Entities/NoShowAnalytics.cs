using HospitalNoShow.Domain.Enums;

namespace HospitalNoShow.Domain.Entities;

public class NoShowAnalytics
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }

    // Risk score: 0.0 (kesinlikle gelir) - 1.0 (kesinlikle gelmez)
    public double RiskScore { get; set; }
    public string RiskLevel => RiskScore switch
    {
        <= 0.3 => "Low",
        <= 0.6 => "Medium",
        <= 0.8 => "High",
        _ => "Critical"
    };

    // Faktörler ve ağırlıkları
    public double PreviousNoShowRateWeight { get; set; }   // Önceki gelmeme oranı (en yüksek ağırlık)
    public double AgeGroupWeight { get; set; }              // Yaş grubunun etkisi
    public double DistanceWeight { get; set; }              // Hastaneye uzaklık etkisi
    public double AppointmentTimeWeight { get; set; }       // Randevu saatinin etkisi
    public double AppointmentDayWeight { get; set; }        // Haftanın günü etkisi
    public double WeatherWeight { get; set; }               // Hava durumu etkisi
    public double SmsResponseWeight { get; set; }           // SMS cevabının etkisi
    public double InsuranceTypeWeight { get; set; }         // Sigorta tipi etkisi
    public double FirstVisitWeight { get; set; }            // İlk ziyaret etkisi

    // Parametre değerleri
    public WeatherCondition WeatherCondition { get; set; } = WeatherCondition.Unknown;
    public SmsResponseType SmsResponse { get; set; } = SmsResponseType.NotSent;
    public DateTime? SmsSentAt { get; set; }
    public DateTime? SmsRespondedAt { get; set; }
    public bool IsReminderSent { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Appointment Appointment { get; set; } = null!;
}
