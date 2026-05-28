using ClinicOps.API.DTOs.Auth;

namespace ClinicOps.Application.Services.Auth
{
    public interface IAuthLoginService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> VerifyMfaLoginAsync(VerifyLoginMfaRequest request);
    }
}
