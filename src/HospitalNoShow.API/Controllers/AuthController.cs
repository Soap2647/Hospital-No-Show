using HospitalNoShow.Application.DTOs.Auth;
using HospitalNoShow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalNoShow.API.Controllers;

/// <summary>
/// Kimlik doğrulama ve kayıt işlemleri.
/// </summary>
public class AuthController(IAuthService authService) : BaseApiController
{
    /// <summary>
    /// Tüm roller için ortak giriş endpoint'i.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Admin tarafından kullanılan, ayrı URL ile admin girişi.
    /// Aynı servisi kullanır; rol kontrolü token içinde gerçekleşir.
    /// </summary>
    [HttpPost("admin/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdminLogin(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.IsSuccess && !result.Value!.Roles.Contains("Admin"))
            return Forbid();

        return ToActionResult(result);
    }

    /// <summary>
    /// Doktor girişi - Doktor rolü kontrolü yapılır.
    /// </summary>
    [HttpPost("doctor/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DoctorLogin(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.IsSuccess && !result.Value!.Roles.Contains("Doctor"))
            return Forbid();

        return ToActionResult(result);
    }

    /// <summary>
    /// Hasta girişi - Hasta rolü kontrolü yapılır.
    /// </summary>
    [HttpPost("patient/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatientLogin(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.IsSuccess && !result.Value!.Roles.Contains("Patient"))
            return Forbid();

        return ToActionResult(result);
    }

    /// <summary>
    /// Yeni hasta kaydı.
    /// </summary>
    [HttpPost("register/patient")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPatient(
        [FromBody] RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterPatientAsync(request, cancellationToken);
        return result.IsSuccess ? Created("", result.Value) : ToActionResult(result);
    }

    /// <summary>
    /// Yeni doktor kaydı (Admin yetkisi gerekli).
    /// </summary>
    [HttpPost("register/doctor")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegisterDoctor(
        [FromBody] RegisterDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterDoctorAsync(request, cancellationToken);
        return result.IsSuccess ? Created("", result.Value) : ToActionResult(result);
    }

    /// <summary>
    /// Şifre değiştirme (giriş yapılmış kullanıcı).
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var result = await authService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        return ToActionResult(result);
    }
}

public record ChangePasswordRequest(
    [System.ComponentModel.DataAnnotations.Required] string CurrentPassword,
    [System.ComponentModel.DataAnnotations.Required,
     System.ComponentModel.DataAnnotations.MinLength(8)] string NewPassword
);
