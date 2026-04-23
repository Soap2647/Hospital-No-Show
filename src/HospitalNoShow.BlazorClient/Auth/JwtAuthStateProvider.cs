using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace HospitalNoShow.BlazorClient.Auth;

/// <summary>
/// Blazor'un authentication state'ini JWT token üzerinden yönetir.
/// </summary>
public class JwtAuthStateProvider(TokenService tokenService) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var isValid = await tokenService.IsTokenValidAsync();
        if (!isValid)
            return Anonymous;

        var token = await tokenService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return Anonymous;

        var claims = TokenService.ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt", "fullName", "role");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    /// <summary>
    /// Giriş yapıldığında çağrılır; Blazor'u yeni state ile bilgilendirir.
    /// </summary>
    public void NotifyUserLoggedIn(string token)
    {
        var claims = TokenService.ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt", "fullName", "role");
        var user = new ClaimsPrincipal(identity);
        var state = new AuthenticationState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    /// <summary>
    /// Çıkış yapıldığında çağrılır.
    /// </summary>
    public void NotifyUserLoggedOut()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}
