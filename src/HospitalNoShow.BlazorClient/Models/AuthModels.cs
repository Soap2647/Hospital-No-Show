using System.ComponentModel.DataAnnotations;

namespace HospitalNoShow.BlazorClient.Models;

public class LoginModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    public string Password { get; set; } = string.Empty;
}

public record AuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string UserId,
    string Email,
    string FullName,
    List<string> Roles
);

public class UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public string Token { get; set; } = string.Empty;

    public bool IsAdmin => Roles.Contains("Admin");
    public bool IsDoctor => Roles.Contains("Doctor");
    public bool IsPatient => Roles.Contains("Patient");

    public string PrimaryRole => IsAdmin ? "Admin" : IsDoctor ? "Doctor" : "Patient";
}
