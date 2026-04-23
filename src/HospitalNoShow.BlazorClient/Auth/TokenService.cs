using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using HospitalNoShow.BlazorClient.Models;

namespace HospitalNoShow.BlazorClient.Auth;

/// <summary>
/// LocalStorage'daki JWT token'ı yönetir.
/// </summary>
public class TokenService(ILocalStorageService localStorage)
{
    private const string TokenKey = "hnshow_token";
    private const string SessionKey = "hnshow_session";

    public async Task SaveSessionAsync(AuthResponse response)
    {
        var session = new UserSession
        {
            UserId = response.UserId,
            Email = response.Email,
            FullName = response.FullName,
            Roles = response.Roles,
            Token = response.AccessToken
        };

        await localStorage.SetItemAsync(TokenKey, response.AccessToken);
        await localStorage.SetItemAsync(SessionKey, session);
    }

    public async Task<string?> GetTokenAsync()
        => await localStorage.GetItemAsync<string>(TokenKey);

    public async Task<UserSession?> GetSessionAsync()
        => await localStorage.GetItemAsync<UserSession>(SessionKey);

    public async Task ClearAsync()
    {
        await localStorage.RemoveItemAsync(TokenKey);
        await localStorage.RemoveItemAsync(SessionKey);
    }

    public async Task<bool> IsTokenValidAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return false;

        try
        {
            var claims = ParseClaimsFromJwt(token);
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (expClaim is null) return false;
            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
            return exp > DateTimeOffset.UtcNow.AddMinutes(1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// JWT payload'ını Base64Url decode ederek claim'leri çıkarır.
    /// System.IdentityModel.Tokens.Jwt bağımlılığı gerektirmez.
    /// </summary>
    public static IEnumerable<Claim> ParseClaimsFromJwt(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return [];

            var payload = parts[1];
            // Base64Url → Base64
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);

            var claims = new List<Claim>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in prop.Value.EnumerateArray())
                        claims.Add(new Claim(prop.Name, item.GetString() ?? string.Empty));
                }
                else
                {
                    claims.Add(new Claim(prop.Name, prop.Value.ToString()));
                }
            }
            return claims;
        }
        catch
        {
            return [];
        }
    }
}
