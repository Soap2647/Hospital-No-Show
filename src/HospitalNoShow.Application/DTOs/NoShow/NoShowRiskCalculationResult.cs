namespace HospitalNoShow.Application.DTOs.NoShow;

public record NoShowRiskCalculationResult(
    double RiskScore,
    string RiskLevel,
    double PreviousNoShowRateWeight,
    double AgeGroupWeight,
    double DistanceWeight,
    double AppointmentTimeWeight,
    double AppointmentDayWeight,
    double InsuranceTypeWeight,
    double FirstVisitWeight,
    string RiskExplanation
);
