using ClinicOps.API.DTOs.Clinic;
using ClinicOps.Application.Services.ClinicSettings;
using ClinicOps.Application.Services.Common;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.Application.Services.ClinicProfile
{
    public class ClinicProfileService : IClinicProfileService
    {
        private readonly ApplicationDbContext _db;
        private readonly IClinicContextService _clinicContext;
        private readonly IProfileImageStorage _imageStorage;

        public ClinicProfileService(
            ApplicationDbContext db,
            IClinicContextService clinicContext,
            IProfileImageStorage imageStorage)
        {
            _db = db;
            _clinicContext = clinicContext;
            _imageStorage = imageStorage;
        }

        public async Task<ClinicProfileDto> GetProfileAsync(ClaimsPrincipal user)
        {
            var clinicId = RequireTokenClinicId(user, "Only clinic users can access clinic profile. Login with a clinic account.");
            var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clinicId);
            if (clinic == null)
                throw new KeyNotFoundException("Clinic not found.");
            return MapProfile(clinic);
        }

        public async Task<ClinicProfileDto> UpdateProfileAsync(UpdateClinicProfileRequest request, ClaimsPrincipal user)
        {
            var clinicId = RequireTokenClinicId(user, "Only clinic users can update clinic profile.");
            var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);
            if (clinic == null)
                throw new KeyNotFoundException("Clinic not found.");

            if (request.Name != null) clinic.Name = request.Name;
            if (request.Address != null) clinic.Address = request.Address;
            if (request.Phone != null) clinic.Phone = request.Phone;
            if (request.LogoUrl != null) clinic.LogoUrl = request.LogoUrl;
            if (request.Description != null) clinic.Description = request.Description;
            ClinicPreferencesMapper.ApplyVital(request.VitalPreferences, clinic);
            ClinicPreferencesMapper.ApplyProtocol(request.ProtocolPreferences, clinic);
            ClinicPreferencesMapper.ApplyColorTheme(request.ColorThemePreferences, clinic);

            await _db.SaveChangesAsync();
            return MapProfile(clinic);
        }

        public async Task<ClinicProfileDto> UploadLogoAsync(
            IFormFile? file,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var clinicId = RequireTokenClinicId(user, "Only clinic users can upload clinic logo.");
            var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);
            if (clinic == null)
                throw new KeyNotFoundException("Clinic not found.");

            clinic.LogoUrl = await _imageStorage.SaveImageAsync(
                file!,
                $"uploads/clinics/{clinicId}",
                "logo",
                cancellationToken);

            await _db.SaveChangesAsync();
            return MapProfile(clinic);
        }

        private Guid RequireTokenClinicId(ClaimsPrincipal user, string missingMessage)
        {
            var clinicId = _clinicContext.GetClinicIdFromToken(user);
            if (!clinicId.HasValue)
                throw new InvalidOperationException(missingMessage);
            return clinicId.Value;
        }

        private static ClinicProfileDto MapProfile(Clinic clinic) => new()
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Address = clinic.Address,
            Phone = clinic.Phone,
            LogoUrl = clinic.LogoUrl,
            Description = clinic.Description,
            ClinicMode = clinic.ClinicMode,
            CreatedAt = clinic.CreatedAt,
            IsActive = clinic.IsActive,
            VitalPreferences = ClinicPreferencesMapper.ToVitalDto(clinic),
            ProtocolPreferences = ClinicPreferencesMapper.ToProtocolDto(clinic),
            ColorThemePreferences = ClinicPreferencesMapper.ToColorThemeDto(clinic),
        };
    }
}
