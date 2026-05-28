using ClinicOps.API.DTOs.Auth;

namespace ClinicOps.Application.Services.Auth
{
    public interface IAuthMfaService
    {
        Task<MfaSetupResponse> GenerateSetupAsync(string userId);
        Task<MfaEnabledResponse> EnableAsync(string userId, string code);
    }
}
