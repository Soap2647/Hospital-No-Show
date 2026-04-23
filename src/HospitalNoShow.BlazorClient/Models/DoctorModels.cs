namespace HospitalNoShow.BlazorClient.Models;

public record DoctorResponse(
    int Id,
    string UserId,
    string FullName,
    string Email,
    string Title,
    string Specialty,
    string PolyclinicName,
    string Department,
    int MaxDailyPatients,
    int AverageAppointmentDurationMinutes
)
{
    public string DisplayName => $"{Title} {FullName}";
}

public record NoShowAnalyticsResponse(
    int Id,
    int AppointmentId,
    double RiskScore,
    string RiskLevel,
    double PreviousNoShowRateWeight,
    double AgeGroupWeight,
    double DistanceWeight,
    double AppointmentTimeWeight,
    double AppointmentDayWeight,
    double InsuranceTypeWeight,
    double FirstVisitWeight,
    string WeatherCondition,
    string SmsResponse,
    bool IsReminderSent,
    DateTime CalculatedAt
);
