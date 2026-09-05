using ClinicOps.API.DTOs.DoctorProfile;
using Microsoft.AspNetCore.Http;

namespace ClinicOps.Application.Services.DoctorProfile
{
    public interface IDoctorProfileService
    {
        Task<DoctorProfileDto> GetProfileAsync(string userId);
        Task<DoctorProfileDto> UpdateProfileAsync(string userId, UpdateDoctorProfileRequest request);
        Task<DoctorProfileDto> UploadSignatureAsync(string userId, IFormFile? file, CancellationToken cancellationToken = default);
        Task<DoctorProfileDto> UploadStampAsync(string userId, IFormFile? file, CancellationToken cancellationToken = default);
    }
}
