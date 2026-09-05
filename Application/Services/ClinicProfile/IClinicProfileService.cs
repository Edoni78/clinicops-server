using ClinicOps.API.DTOs.Clinic;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ClinicOps.Application.Services.ClinicProfile
{
    public interface IClinicProfileService
    {
        Task<ClinicProfileDto> GetProfileAsync(ClaimsPrincipal user);
        Task<ClinicProfileDto> UpdateProfileAsync(UpdateClinicProfileRequest request, ClaimsPrincipal user);
        Task<ClinicProfileDto> UploadLogoAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    }
}
