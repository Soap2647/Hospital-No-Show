using HospitalNoShow.BlazorClient.Models;

namespace HospitalNoShow.BlazorClient.Services;

public class PatientApiService(ApiService api)
{
    public async Task<ApiResult<PatientResponse>> GetMeAsync()
        => await api.GetAsync<PatientResponse>("api/patients/me");

    public async Task<ApiResult<List<PatientResponse>>> GetAllAsync()
        => await api.GetAsync<List<PatientResponse>>("api/patients");

    public async Task<ApiResult<PatientResponse>> GetByIdAsync(int id)
        => await api.GetAsync<PatientResponse>($"api/patients/{id}");

    public async Task<ApiResult<List<PatientResponse>>> GetHighRiskAsync(double minRate = 0.3)
        => await api.GetAsync<List<PatientResponse>>($"api/patients/high-risk?minNoShowRate={minRate}");
}
