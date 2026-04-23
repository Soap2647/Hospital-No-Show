using HospitalNoShow.BlazorClient.Models;

namespace HospitalNoShow.BlazorClient.Services;

public class NoShowApiService(ApiService api)
{
    public async Task<ApiResult<NoShowAnalyticsResponse>> GetByAppointmentAsync(int appointmentId)
        => await api.GetAsync<NoShowAnalyticsResponse>($"api/noshowanalytics/appointment/{appointmentId}");

    public async Task<ApiResult<List<NoShowAnalyticsResponse>>> GetHighRiskAsync(double minScore = 0.6)
        => await api.GetAsync<List<NoShowAnalyticsResponse>>($"api/noshowanalytics/high-risk?minRiskScore={minScore}");

    public async Task<ApiResult<object>> RecalculateAsync(int appointmentId)
        => await api.PostAsync<object>($"api/noshowanalytics/appointment/{appointmentId}/recalculate", new { });

    public async Task<ApiResult<object>> GetDoctorAverageRiskAsync(int doctorId)
        => await api.GetAsync<object>($"api/noshowanalytics/doctor/{doctorId}/average-risk");
}
