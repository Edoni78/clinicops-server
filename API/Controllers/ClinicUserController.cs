using ClinicOps.API.DTOs.ClinicUser;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ClinicAdmin,SuperAdmin")]
    public class ClinicUserController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private static readonly string[] AllowedRoles = { "Doctor", "Nurse", "LabTechnician" };

        public ClinicUserController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// List users for the clinic. ClinicAdmin sees their clinic; SuperAdmin can pass clinicId query. Optional role filter.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ClinicUserListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ClinicUserListItemDto>>> List(
            [FromQuery] Guid? clinicId = null,
            [FromQuery] string? role = null)
        {
            var (_, resolvedClinicId) = await ResolveClinicIdAsync(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("ClinicId required for SuperAdmin, or login as ClinicAdmin.");

            var users = await _userManager.Users
                .Where(u => u.ClinicId == resolvedClinicId.Value)
                .ToListAsync();

            var result = new List<ClinicUserListItemDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var r = roles.FirstOrDefault(AllowedRoles.Contains) ?? roles.FirstOrDefault();
                if (r == "ClinicAdmin") continue;
                result.Add(new ClinicUserListItemDto
                {
                    Id = u.Id,
                    Email = u.Email!,
                    Role = r ?? "",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                });
            }

            if (!string.IsNullOrEmpty(role) && AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                result = result.Where(x => x.Role.Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(result.OrderBy(x => x.Role).ThenBy(x => x.Email).ToList());
        }

        /// <summary>
        /// Create a clinic user (Doctor, Nurse, or LabTechnician). ClinicAdmin: uses their clinic. SuperAdmin: pass clinicId in body or query.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClinicUserListItemDto>> Create(
            [FromBody] CreateClinicUserRequest request,
            [FromQuery] Guid? clinicId = null)
        {
            var (_, resolvedClinicId) = await ResolveClinicIdAsync(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("ClinicId required for SuperAdmin, or login as ClinicAdmin.");

            var role = request.Role?.Trim();
            if (string.IsNullOrEmpty(role) || !AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return BadRequest($"Role must be one of: {string.Join(", ", AllowedRoles)}.");

            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return BadRequest("Email already in use.");

            var clinic = await _db.Clinics.FindAsync(resolvedClinicId.Value);
            if (clinic == null)
                return BadRequest("Clinic not found.");

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                NormalizedUserName = request.Email.ToUpperInvariant(),
                NormalizedEmail = request.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                ClinicId = resolvedClinicId.Value,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
                return BadRequest(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, role);

            return CreatedAtAction(nameof(List), new ClinicUserListItemDto
            {
                Id = user.Id,
                Email = user.Email!,
                Role = role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        private async Task<(bool isSuperAdmin, Guid? clinicId)> ResolveClinicIdAsync(Guid? fromQuery = null)
        {
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            if (!string.IsNullOrEmpty(clinicIdClaim) && Guid.TryParse(clinicIdClaim, out var fromToken))
                return (false, fromToken);
            if (fromQuery.HasValue)
                return (true, fromQuery.Value);
            var defaultId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var clinic = await _db.Clinics.FindAsync(defaultId);
            if (clinic != null)
                return (true, defaultId);
            return (true, null);
        }
    }
}
