using ClinicOps.API.DTOs.ClinicApplication;
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
    [Authorize(Roles = "SuperAdmin")]
    public class ClinicApplicationController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClinicApplicationController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// List clinic applications. Optional status filter: Pending, Approved, Rejected.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ClinicApplicationDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ClinicApplicationDto>>> List([FromQuery] string? status = null)
        {
            var query = _db.ClinicApplications.AsNoTracking();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var statusEnum))
                query = query.Where(a => a.Status == statusEnum);

            var list = await query
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => new ClinicApplicationDto
                {
                    Id = a.Id,
                    ClinicName = a.ClinicName,
                    AdminEmail = a.AdminEmail,
                    Status = a.Status,
                    ClinicMode = a.ClinicMode,
                    CreatedAtUtc = a.CreatedAtUtc,
                    ReviewedAtUtc = a.ReviewedAtUtc,
                    ReviewNote = a.ReviewNote
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Approve a clinic application. Creates the clinic and clinic admin user so they can login.
        /// </summary>
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRejectRequest? request = null)
        {
            var app = await _db.ClinicApplications.FirstOrDefaultAsync(a => a.Id == id);
            if (app == null)
                return NotFound("Application not found.");

            if (app.Status != ApplicationStatus.Pending)
                return BadRequest($"Application is already {app.Status}. Only pending applications can be approved.");

            var existingUser = await _userManager.FindByEmailAsync(app.AdminEmail);
            if (existingUser != null)
                return BadRequest("A user with this email already exists. Cannot approve.");

            var clinic = new Clinic
            {
                Name = app.ClinicName,
                ClinicMode = app.ClinicMode,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _db.Clinics.Add(clinic);
            await _db.SaveChangesAsync();

            var adminUser = new ApplicationUser
            {
                UserName = app.AdminEmail,
                Email = app.AdminEmail,
                NormalizedUserName = app.AdminEmail.ToUpperInvariant(),
                NormalizedEmail = app.AdminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                ClinicId = clinic.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            adminUser.PasswordHash = app.AdminPasswordHash;
            var createResult = await _userManager.CreateAsync(adminUser);
            if (!createResult.Succeeded)
            {
                _db.Clinics.Remove(clinic);
                await _db.SaveChangesAsync();
                return BadRequest(string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(adminUser, "ClinicAdmin");

            app.Status = ApplicationStatus.Approved;
            app.ReviewedAtUtc = DateTime.UtcNow;
            app.ReviewNote = request?.ReviewNote;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Application approved. Clinic and admin user created. They can now login with the email and password they used when applying.",
                clinicId = clinic.Id,
                adminUserId = adminUser.Id
            });
        }

        /// <summary>
        /// Reject a clinic application.
        /// </summary>
        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, [FromBody] ApproveRejectRequest? request = null)
        {
            var app = await _db.ClinicApplications.FirstOrDefaultAsync(a => a.Id == id);
            if (app == null)
                return NotFound("Application not found.");

            if (app.Status != ApplicationStatus.Pending)
                return BadRequest($"Application is already {app.Status}. Only pending applications can be rejected.");

            app.Status = ApplicationStatus.Rejected;
            app.ReviewedAtUtc = DateTime.UtcNow;
            app.ReviewNote = request?.ReviewNote;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Application rejected." });
        }
    }
}
