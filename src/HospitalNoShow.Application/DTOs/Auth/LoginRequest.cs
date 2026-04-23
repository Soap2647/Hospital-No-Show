using System.ComponentModel.DataAnnotations;

namespace HospitalNoShow.Application.DTOs.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);
