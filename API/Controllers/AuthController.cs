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

        [HttpPost("register-clinic")]
        public async Task<ActionResult<AuthResponse>> RegisterClinic(
            [FromBody] RegisterClinicRequest req)
        {
            var existing = await _userManager.FindByEmailAsync(req.Email);
            if (existing != null)
                return BadRequest("Email already in use.");

            var clinic = new Clinic { Name = req.ClinicName };
            _db.Clinics.Add(clinic);
            await _db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = req.Email,
                Email = req.Email,
                ClinicId = clinic.Id
            };

            var createRes = await _userManager.CreateAsync(user, req.Password);
            if (!createRes.Succeeded)
                return BadRequest(createRes.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, "ClinicAdmin");

            var roles = await _userManager.GetRolesAsync(user);
            var (token, exp) = _jwt.CreateToken(user, roles);

            return Ok(new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = exp,
                User = new AuthClinicUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    ClinicId = clinic.Id.ToString(),
                    ClinicName = clinic.Name
                }
            });
        }

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
                    false
                );

            if (!validPassword.Succeeded)
                return Unauthorized("Invalid email or password.");

            var roles = await _userManager.GetRolesAsync(user);
            var (token, exp) = _jwt.CreateToken(user, roles);

            return Ok(new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = exp,
                User = new AuthClinicUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    ClinicId = user.ClinicId.ToString(),
                    ClinicName = user.Clinic.Name
                }
            });
        }
    }
}
