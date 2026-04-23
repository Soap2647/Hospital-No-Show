using HospitalNoShow.Domain.Entities;

namespace HospitalNoShow.Application.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(ApplicationUser user, IReadOnlyList<string> roles);
    int GetExpirationSeconds();
}
