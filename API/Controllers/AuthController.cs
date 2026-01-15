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
        public async Task<ActionResult<AuthResponse>> RegisterClinic([FromBody] RegisterClinicRequest req)
        {
            // 1) Prevent duplicate email
            var existing = await _userManager.FindByEmailAsync(req.Email);
            if (existing != null) return BadRequest("Email already in use.");

            // 2) Create Clinic
            var clinic = new Clinic { Name = req.ClinicName };
            _db.Clinics.Add(clinic);
            await _db.SaveChangesAsync();

            // 3) Create ClinicAdmin user linked to Clinic
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

            // 4) Issue JWT
            var roles = await _userManager.GetRolesAsync(user);
            var (token, exp) = _jwt.CreateToken(user, roles);

            return Ok(new AuthResponse { AccessToken = token, ExpiresAtUtc = exp });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        {
            var user = await _userManager.Users
                .Include(u => u.Clinic)
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            var signIn = await _signInManager.CheckPasswordSignInAsync(
                user,
                req.Password,
                lockoutOnFailure: false
            );

            if (!signIn.Succeeded)
                return Unauthorized("Invalid credentials.");

            var roles = await _userManager.GetRolesAsync(user);

            var (token, exp) = _jwt.CreateToken(user, roles);

            return Ok(new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = exp
            });
        }
    }
}
