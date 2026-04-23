using HospitalNoShow.Application.Common;
using HospitalNoShow.Application.DTOs.Auth;

namespace HospitalNoShow.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RegisterPatientAsync(
        RegisterPatientRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RegisterDoctorAsync(
        RegisterDoctorRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
