using HospitalNoShow.BlazorClient.Models;

namespace HospitalNoShow.BlazorClient.Services;

public class DoctorApiService(ApiService api)
{
    public async Task<ApiResult<List<DoctorResponse>>> GetAllAsync()
        => await api.GetAsync<List<DoctorResponse>>("api/doctors");

    public async Task<ApiResult<DoctorResponse>> GetByIdAsync(int id)
        => await api.GetAsync<DoctorResponse>($"api/doctors/{id}");

    public async Task<ApiResult<DoctorResponse>> GetByUserIdAsync(string userId)
        => await api.GetAsync<DoctorResponse>($"api/doctors/by-user/{Uri.EscapeDataString(userId)}");

    public async Task<ApiResult<List<DoctorResponse>>> GetBySpecialtyAsync(string specialty)
        => await api.GetAsync<List<DoctorResponse>>($"api/doctors/by-specialty?specialty={Uri.EscapeDataString(specialty)}");
}
