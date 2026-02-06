using ClinicOps.API.DTOs.Auth;
using ClinicOps.Application.Services.Auth;
using ClinicOps.Domain.Entities;
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

        public AuthController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwt)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwt;
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
                return Unauthorized("Invalid email or password.");

            var validPassword =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: false
                );

            if (!validPassword.Succeeded)
                return Unauthorized("Invalid email or password.");

            var roles = await _userManager.GetRolesAsync(user);

            // ✅ CAPTURE ALL 3 VALUES
            var (token, exp, role) = _jwt.CreateToken(user, roles);

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
                AdminPasswordHash = passwordHash
            };

            _db.ClinicApplications.Add(app);
            await _db.SaveChangesAsync();

            return Ok("Application submitted successfully.");
        }
    }
}
