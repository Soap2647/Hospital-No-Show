using HospitalNoShow.BlazorClient.Models;

namespace HospitalNoShow.BlazorClient.Services;

public class AppointmentApiService(ApiService api)
{
    public async Task<ApiResult<AppointmentResponse>> CreateAsync(CreateAppointmentModel model)
    {
        var date = DateOnly.FromDateTime(model.AppointmentDate!.Value);
        var time = TimeOnly.FromTimeSpan(model.AppointmentTime!.Value);

        return await api.PostAsync<AppointmentResponse>(
            "api/appointments",
            new
            {
                model.DoctorId,
                AppointmentDate = date.ToString("yyyy-MM-dd"),
                AppointmentTime = time.ToString("HH:mm"),
                model.Notes
            });
    }

    public async Task<ApiResult<AppointmentResponse>> GetByIdAsync(int id)
        => await api.GetAsync<AppointmentResponse>($"api/appointments/{id}");

    public async Task<ApiResult<List<AppointmentResponse>>> GetMyAppointmentsAsync()
        => await api.GetAsync<List<AppointmentResponse>>("api/appointments/my");

    public async Task<ApiResult<List<AppointmentResponse>>> GetByPatientAsync(int patientId)
        => await api.GetAsync<List<AppointmentResponse>>($"api/appointments/patient/{patientId}");

    public async Task<ApiResult<List<AppointmentResponse>>> GetByDoctorAndDateAsync(int doctorId, DateOnly date)
        => await api.GetAsync<List<AppointmentResponse>>(
            $"api/appointments/doctor/{doctorId}/date/{date:yyyy-MM-dd}");

    public async Task<ApiResult> UpdateStatusAsync(int id, AppointmentStatus status, string? reason = null)
        => await api.PatchAsync($"api/appointments/{id}/status", new UpdateStatusRequest(status, reason));

    public async Task<ApiResult> CancelAsync(int id, string reason)
        => await api.DeleteAsync($"api/appointments/{id}", new CancelRequest(reason));

    // Admin: tüm randevular (isteğe bağlı filtre)
    public async Task<ApiResult<List<AppointmentResponse>>> GetAllForAdminAsync(AppointmentStatus? status = null)
        => await api.GetAsync<List<AppointmentResponse>>(
            status.HasValue ? $"api/appointments?status={(int)status}" : "api/appointments");

    // Admin: hafif stats özeti (chart + KPI)
    public async Task<ApiResult<AdminStatsResponse>> GetAdminStatsAsync()
        => await api.GetAsync<AdminStatsResponse>("api/appointments/stats");
}

// DTO
public record AdminStatsResponse(
    int TotalCount,
    int TodayCount,
    int CompletedCount,
    int NoShowCount,
    int ScheduledCount,
    int CancelledCount,
    int HighRiskCount,
    double NoShowRate,
    List<PolyclinicStatItem> PolyclinicStats
);

public record PolyclinicStatItem(string? Name, double NoShowRate);
