using ClinicOps.API.DTOs.ClinicUser;
using ClinicOps.Application.Services.Common;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.Application.Services.ClinicUsers
{
    public class ClinicUserService : IClinicUserService
    {
        private static readonly string[] AllowedRoles = { "Doctor", "Nurse", "LabTechnician" };

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClinicContextService _clinicContext;

        public ClinicUserService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IClinicContextService clinicContext)
        {
            _db = db;
            _userManager = userManager;
            _clinicContext = clinicContext;
        }

        public async Task<List<ClinicUserListItemDto>> ListAsync(
            ClaimsPrincipal user,
            Guid? clinicIdQuery = null,
            string? role = null)
        {
            var clinicId = await RequireClinicIdAsync(user, clinicIdQuery);

            var users = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.ClinicId == clinicId && u.IsActive)
                .ToListAsync();

            var result = new List<ClinicUserListItemDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var r = roles.FirstOrDefault(AllowedRoles.Contains) ?? roles.FirstOrDefault();
                if (r == "ClinicAdmin") continue;
                result.Add(MapDto(u, r ?? ""));
            }

            if (!string.IsNullOrEmpty(role) && AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                result = result.Where(x => x.Role.Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();

            return result.OrderBy(x => x.Role).ThenBy(x => x.DisplayName).ToList();
        }

        public async Task<ClinicUserListItemDto> CreateAsync(
            CreateClinicUserRequest request,
            ClaimsPrincipal user,
            Guid? clinicIdQuery = null)
        {
            var clinicId = await RequireClinicIdAsync(user, clinicIdQuery);

            var role = request.Role?.Trim();
            if (string.IsNullOrEmpty(role) || !AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Role must be one of: {string.Join(", ", AllowedRoles)}.");

            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                throw new InvalidOperationException("Email already in use.");

            var clinic = await _db.Clinics.FindAsync(clinicId);
            if (clinic == null)
                throw new InvalidOperationException("Clinic not found.");

            clinic.EnsureStaffRoleAllowed(role);

            var displayName = request.DisplayName?.Trim();
            if (string.IsNullOrEmpty(displayName))
                throw new InvalidOperationException("DisplayName is required.");
            if (displayName.Length > 200)
                throw new InvalidOperationException("DisplayName must be at most 200 characters.");

            var newUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                NormalizedUserName = request.Email.ToUpperInvariant(),
                NormalizedEmail = request.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                ClinicId = clinicId,
                DoctorDisplayName = displayName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(newUser, request.Password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(newUser, role);
            return MapDto(newUser, role);
        }

        public async Task DeleteAsync(string userId, ClaimsPrincipal user, Guid? clinicIdQuery = null)
        {
            var clinicId = await RequireClinicIdAsync(user, clinicIdQuery);

            var target = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (target == null)
                throw new KeyNotFoundException("User not found.");

            if (target.ClinicId != clinicId)
                throw new KeyNotFoundException("User not found in this clinic.");

            var roles = await _userManager.GetRolesAsync(target);
            var isAllowedStaff = roles.Any(r => AllowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            if (!isAllowedStaff)
                throw new InvalidOperationException("Only clinic staff users can be deleted from this endpoint.");

            var result = await _userManager.DeleteAsync(target);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        private async Task<Guid> RequireClinicIdAsync(ClaimsPrincipal user, Guid? clinicIdQuery)
        {
            var (_, clinicId) = await _clinicContext.TryResolveClinicIdAsync(user, clinicIdQuery);
            if (!clinicId.HasValue)
                throw new InvalidOperationException("ClinicId required for SuperAdmin, or login as ClinicAdmin.");
            return clinicId.Value;
        }

        private static ClinicUserListItemDto MapDto(ApplicationUser u, string role) => new()
        {
            Id = u.Id,
            Email = u.Email!,
            DisplayName = u.DoctorDisplayName ?? u.Email ?? u.UserName ?? u.Id,
            Role = role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}
