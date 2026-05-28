using ClinicOps.API.DTOs.Auth;
using ClinicOps.Application.Services.Auth;
using ClinicOps.Application.Services.Gdpr;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwt;
        private readonly IAuditLogService _auditLogService;

        public AuthController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwt,
            IAuditLogService auditLogService)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwt;
            _auditLogService = auditLogService;
        }

        // =====================================================
        // LOGIN (SUPPORTS CLINIC USERS + SUPERADMIN)
        // =====================================================
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginRequest request)
        {
            var user = await _userManager.Users
                .Include(u => u.Clinic)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                await _auditLogService.TryLogAsync(
                    action: "FailedLogin",
                    entityName: "Auth",
                    entityId: null,
                    clinicId: null,
                    userId: null,
                    status: "Failed",
                    severity: "Warning",
                    description: $"Failed login attempt for email {request.Email}.");
                return Unauthorized("Invalid email or password.");
            }

            var validPassword =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: false
                );

            if (!validPassword.Succeeded)
            {
                await _auditLogService.TryLogAsync(
                    action: "FailedLogin",
                    entityName: "Auth",
                    entityId: null,
                    clinicId: user.ClinicId,
                    userId: user.Id,
                    status: "Failed",
                    severity: "Warning",
                    description: $"Failed login attempt for user {user.Email}.");
                return Unauthorized("Invalid email or password.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            // ✅ CAPTURE ALL 3 VALUES
            var (token, exp, role) = _jwt.CreateToken(user, roles);
            await _auditLogService.TryLogAsync(
                action: "Login",
                entityName: "Auth",
                entityId: null,
                clinicId: user.ClinicId,
                userId: user.Id,
                status: "Success",
                severity: "Info",
                description: "User logged into the system successfully.");

            return Ok(new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = exp,
                User = new AuthClinicUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    ClinicId = user.ClinicId?.ToString(),
                    ClinicName = user.Clinic?.Name,
                    Role = role
                }
            });
        }

        // =====================================================
        // APPLY FOR CLINIC (NO USER CREATED)
        // =====================================================
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyForClinic(
            [FromBody] RegisterClinicRequest req)
        {
            if (!Enum.IsDefined(typeof(ClinicMode), req.ClinicMode))
                return BadRequest("clinicMode is required and must be either SoloDoctor or FullTeam.");

            var existsUser = await _userManager.FindByEmailAsync(req.Email);
            if (existsUser != null)
                return BadRequest("Email already in use.");

            var hasPending =
                await _db.ClinicApplications.AnyAsync(a =>
                    a.AdminEmail == req.Email &&
                    a.Status == ApplicationStatus.Pending);

            if (hasPending)
                return BadRequest("You already have a pending application.");

            var passwordHash =
                _userManager.PasswordHasher.HashPassword(null!, req.Password);

            var app = new ClinicApplication
            {
                ClinicName = req.ClinicName,
                AdminEmail = req.Email,
                AdminPasswordHash = passwordHash,
                ClinicMode = req.ClinicMode
            };

            _db.ClinicApplications.Add(app);
            await _db.SaveChangesAsync();

            return Ok("Application submitted successfully.");
        }
    }
}
