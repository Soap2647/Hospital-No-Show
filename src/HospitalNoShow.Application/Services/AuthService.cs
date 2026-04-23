using HospitalNoShow.Application.Common;
using HospitalNoShow.Application.DTOs.Auth;
using HospitalNoShow.Application.Interfaces;
using HospitalNoShow.Domain.Entities;
using HospitalNoShow.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HospitalNoShow.Application.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    ILogger<AuthService> logger) : IAuthService
{
    private const string PatientRole = "Patient";
    private const string DoctorRole = "Doctor";
    private const string AdminRole = "Admin";

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("E-posta veya şifre hatalı.");

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            return Result<AuthResponse>.Failure("E-posta veya şifre hatalı.");
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList().AsReadOnly();
        var token = await jwtTokenService.GenerateTokenAsync(user, roles);

        logger.LogInformation("User logged in: {UserId}, Roles: {Roles}", user.Id, string.Join(",", roles));

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: jwtTokenService.GetExpirationSeconds(),
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Roles: roles
        ));
    }

    public async Task<Result<AuthResponse>> RegisterPatientAsync(
        RegisterPatientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Result<AuthResponse>.Failure("Bu e-posta adresi zaten kullanılıyor.");

        var existingPatient = await unitOfWork.Patients
            .GetByIdentityNumberAsync(request.IdentityNumber, cancellationToken);
        if (existingPatient is not null)
            return Result<AuthResponse>.Failure("Bu TC kimlik numarası zaten kayıtlı.");

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true // Development için otomatik onay
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => e.Description).ToList();
            return Result<AuthResponse>.Failure(errors);
        }

        await userManager.AddToRoleAsync(user, PatientRole);

        var patient = new Patient
        {
            UserId = user.Id,
            IdentityNumber = request.IdentityNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            City = request.City,
            DistanceToHospitalKm = request.DistanceToHospitalKm,
            InsuranceType = request.InsuranceType,
            InsurancePolicyNumber = request.InsurancePolicyNumber,
            HasChronicDisease = request.HasChronicDisease,
            ChronicDiseaseNotes = request.ChronicDiseaseNotes,
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            BloodType = request.BloodType
        };

        await unitOfWork.Patients.AddAsync(patient, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("New patient registered: {UserId}", user.Id);

        var token = await jwtTokenService.GenerateTokenAsync(user, [PatientRole]);
        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: jwtTokenService.GetExpirationSeconds(),
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Roles: [PatientRole]
        ));
    }

    public async Task<Result<AuthResponse>> RegisterDoctorAsync(
        RegisterDoctorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Result<AuthResponse>.Failure("Bu e-posta adresi zaten kullanılıyor.");

        var polyclinic = await unitOfWork.Polyclinics.GetByIdAsync(request.PolyclinicId, cancellationToken);
        if (polyclinic is null)
            return Result<AuthResponse>.Failure("Belirtilen poliklinik bulunamadı.");

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => e.Description).ToList();
            return Result<AuthResponse>.Failure(errors);
        }

        await userManager.AddToRoleAsync(user, DoctorRole);

        var doctor = new Doctor
        {
            UserId = user.Id,
            Specialty = request.Specialty,
            Title = request.Title,
            DiplomaCertificateNumber = request.DiplomaCertificateNumber,
            PolyclinicId = request.PolyclinicId,
            MaxDailyPatients = request.MaxDailyPatients,
            AverageAppointmentDurationMinutes = request.AverageAppointmentDurationMinutes
        };

        await unitOfWork.Doctors.AddAsync(doctor, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("New doctor registered: {UserId}", user.Id);

        var token = await jwtTokenService.GenerateTokenAsync(user, [DoctorRole]);
        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: jwtTokenService.GetExpirationSeconds(),
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Roles: [DoctorRole]
        ));
    }

    public async Task<Result> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Result.Failure("Kullanıcı bulunamadı.");

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Failure(errors);
        }

        return Result.Success();
    }
}
