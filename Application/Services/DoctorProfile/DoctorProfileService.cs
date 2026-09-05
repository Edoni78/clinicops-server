using ClinicOps.API.DTOs.DoctorProfile;
using ClinicOps.Application.Services.Common;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.DoctorProfile
{
    public class DoctorProfileService : IDoctorProfileService
    {
        private readonly ApplicationDbContext _db;
        private readonly IProfileImageStorage _imageStorage;

        public DoctorProfileService(ApplicationDbContext db, IProfileImageStorage imageStorage)
        {
            _db = db;
            _imageStorage = imageStorage;
        }

        public async Task<DoctorProfileDto> GetProfileAsync(string userId)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");
            return MapDto(user);
        }

        public async Task<DoctorProfileDto> UpdateProfileAsync(string userId, UpdateDoctorProfileRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (request.DisplayName != null)
                user.DoctorDisplayName = request.DisplayName.Trim().Length > 0 ? request.DisplayName.Trim() : null;

            await _db.SaveChangesAsync();
            return MapDto(user);
        }

        public async Task<DoctorProfileDto> UploadSignatureAsync(
            string userId,
            IFormFile? file,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            user.SignatureUrl = await _imageStorage.SaveImageAsync(
                file!,
                $"uploads/doctors/{userId}",
                "signature",
                cancellationToken);

            await _db.SaveChangesAsync();
            return MapDto(user);
        }

        public async Task<DoctorProfileDto> UploadStampAsync(
            string userId,
            IFormFile? file,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            user.StampUrl = await _imageStorage.SaveImageAsync(
                file!,
                $"uploads/doctors/{userId}",
                "stamp",
                cancellationToken);

            await _db.SaveChangesAsync();
            return MapDto(user);
        }

        private static DoctorProfileDto MapDto(ApplicationUser user) => new()
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DoctorDisplayName ?? user.Email,
            SignatureUrl = user.SignatureUrl,
            StampUrl = user.StampUrl
        };
    }
}
