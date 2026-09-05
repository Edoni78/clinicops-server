using ClinicOps.API.DTOs.Auth;

namespace ClinicOps.Application.Services.Auth
{
    public interface IClinicRegistrationService
    {
        Task ApplyForClinicAsync(RegisterClinicRequest request);
    }
}
