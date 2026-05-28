using ClinicOps.API.DTOs.Auth;
using ClinicOps.Application.Services.Auth;
using ClinicOps.Application.Services.Gdpr;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthLoginService _authLoginService;
        private readonly IAuthMfaService _authMfaService;
        private readonly IAuditLogService _auditLogService;

        public AuthController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IAuthLoginService authLoginService,
            IAuthMfaService authMfaService,
            IAuditLogService auditLogService)
        {
            _db = db;
            _userManager = userManager;
            _authLoginService = authLoginService;
            _authMfaService = authMfaService;
            _auditLogService = auditLogService;
        }

        // =====================================================
        // LOGIN (SUPPORTS CLINIC USERS + SUPERADMIN)
        // =====================================================
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authLoginService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("mfa/verify-login")]
        public async Task<ActionResult<AuthResponse>> VerifyLoginMfa([FromBody] VerifyLoginMfaRequest request)
        {
            try
            {
                var response = await _authLoginService.VerifyMfaLoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("mfa/setup")]
        [Authorize]
        public async Task<ActionResult<MfaSetupResponse>> GenerateMfaSetup()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                var setup = await _authMfaService.GenerateSetupAsync(userId);
                return Ok(setup);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("mfa/enable")]
        [Authorize]
        public async Task<ActionResult<MfaEnabledResponse>> EnableMfa([FromBody] VerifyMfaCodeRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                var result = await _authMfaService.EnableAsync(userId, request.Code);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
