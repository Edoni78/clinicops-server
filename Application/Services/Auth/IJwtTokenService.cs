using ClinicOps.Domain.Entities;

namespace ClinicOps.Application.Services.Auth
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAtUtc) CreateToken(ApplicationUser user, IList<string> roles);
    }
}