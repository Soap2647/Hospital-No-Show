using HospitalNoShow.BlazorClient.Auth;
using HospitalNoShow.BlazorClient.Models;

namespace HospitalNoShow.BlazorClient.Services;

public class AuthApiService(
    ApiService api,
    TokenService tokenService,
    JwtAuthStateProvider authStateProvider)
{
    public async Task<ApiResult<AuthResponse>> LoginAsync(LoginModel model)
    {
        var result = await api.PostAsync<AuthResponse>(
            "api/auth/login",
            new { model.Email, model.Password });

        if (result.IsSuccess && result.Data is not null)
        {
            await tokenService.SaveSessionAsync(result.Data);
            authStateProvider.NotifyUserLoggedIn(result.Data.AccessToken);
        }

        return result;
    }

    public async Task LogoutAsync()
    {
        await tokenService.ClearAsync();
        authStateProvider.NotifyUserLoggedOut();
    }

    public async Task<ApiResult<AuthResponse>> RegisterPatientAsync(object requestData)
    {
        var result = await api.PostAsync<AuthResponse>(
            "api/auth/register-patient",
            requestData);
        
        if (result.IsSuccess && result.Data is not null)
        {
            await tokenService.SaveSessionAsync(result.Data);
            authStateProvider.NotifyUserLoggedIn(result.Data.AccessToken);
        }

        return result;
    }

    /// <summary>
    /// Session varsa token geçerliliğini de kontrol eder.
    /// Token süresi dolmuşsa veya geçersizse session temizlenir ve null döner.
    /// Bu, login ↔ dashboard redirect döngüsünü engeller.
    /// </summary>
    public async Task<UserSession?> GetCurrentSessionAsync()
    {
        var session = await tokenService.GetSessionAsync();
        if (session is null) return null;

        // Token geçerliliğini kontrol et
        var isTokenValid = await tokenService.IsTokenValidAsync();
        if (!isTokenValid)
        {
            // Süresi dolmuş / bozuk token → temizle
            await tokenService.ClearAsync();
            authStateProvider.NotifyUserLoggedOut();
            return null;
        }

        return session;
    }
}
