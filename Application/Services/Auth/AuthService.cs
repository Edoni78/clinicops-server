using ClinicOps.API.DTOs.Auth;
using ClinicOps.Application.Services.Auth;
using ClinicOps.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ClinicOps.Application.Services.Auth
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AuthResponse> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email)
                       ?? throw new Exception("Invalid credentials");

            var validPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!validPassword)
                throw new Exception("Invalid credentials");

            // Roles NOT needed – clinic login only
            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(
                user,
                new List<string>() // empty roles
            );

            return new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAtUtc,
                User = new AuthClinicUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    ClinicId = user.ClinicId.ToString(),
                    ClinicName = user.Clinic.Name
                }
            };
        }
    }
}