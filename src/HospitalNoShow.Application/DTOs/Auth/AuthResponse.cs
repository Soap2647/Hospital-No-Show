namespace HospitalNoShow.Application.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,       // saniye cinsinden
    string UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles
);
