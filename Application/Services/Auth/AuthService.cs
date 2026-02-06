using ClinicOps.API.DTOs.Auth;
using ClinicOps.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Auth
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AuthResponse> LoginAsync(string email, string password)
        {
            var user = await _userManager.Users
                .Include(u => u.Clinic)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                throw new Exception("Invalid credentials");

            var validPassword = await _signInManager.CheckPasswordSignInAsync(
                user,
                password,
                lockoutOnFailure: false
            );

            if (!validPassword.Succeeded)
                throw new Exception("Invalid credentials");

            var roles = await _userManager.GetRolesAsync(user);

            // ✅ tuple destructuring CORRECT
            var (token, expiresAtUtc, role) =
                _jwtTokenService.CreateToken(user, roles);

            return new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc,
                User = new AuthClinicUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    ClinicId = user.ClinicId?.ToString(),
                    ClinicName = user.Clinic?.Name,
                    Role = role
                }
            };
        }
    }
}
