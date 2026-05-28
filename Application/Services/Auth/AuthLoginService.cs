using ClinicOps.API.DTOs.Auth;
using ClinicOps.Application.Services.Gdpr;
using ClinicOps.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClinicOps.Application.Services.Auth
{
    public class AuthLoginService : IAuthLoginService
    {
        private const string MfaTicketPrefix = "mfa_login_ticket_";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwt;
        private readonly IAuditLogService _auditLogService;
        private readonly IMemoryCache _memoryCache;

        public AuthLoginService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwt,
            IAuditLogService auditLogService,
            IMemoryCache memoryCache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwt;
            _auditLogService = auditLogService;
            _memoryCache = memoryCache;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
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
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var validPassword = await _signInManager.CheckPasswordSignInAsync(
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
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                var ticket = Guid.NewGuid().ToString("N");
                _memoryCache.Set(MfaTicketPrefix + ticket, user.Id, TimeSpan.FromMinutes(5));
                return new AuthResponse
                {
                    RequiresMfa = true,
                    MfaTicket = ticket
                };
            }

            var authResponse = await BuildJwtResponseAsync(user);
            await _auditLogService.TryLogAsync(
                action: "Login",
                entityName: "Auth",
                entityId: null,
                clinicId: user.ClinicId,
                userId: user.Id,
                status: "Success",
                severity: "Info",
                description: "User logged into the system successfully.");

            return authResponse;
        }

        public async Task<AuthResponse> VerifyMfaLoginAsync(VerifyLoginMfaRequest request)
        {
            if (!_memoryCache.TryGetValue<string>(MfaTicketPrefix + request.MfaTicket, out var userId) || string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("MFA session expired or invalid.");

            var user = await _userManager.Users
                .Include(u => u.Clinic)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var normalizedCode = request.Code.Replace(" ", "").Replace("-", "");
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                normalizedCode);

            if (!isValid)
                throw new UnauthorizedAccessException("Invalid MFA code.");

            _memoryCache.Remove(MfaTicketPrefix + request.MfaTicket);

            var authResponse = await BuildJwtResponseAsync(user);
            await _auditLogService.TryLogAsync(
                action: "Login",
                entityName: "Auth",
                entityId: null,
                clinicId: user.ClinicId,
                userId: user.Id,
                status: "Success",
                severity: "Info",
                description: "User logged into the system successfully (MFA).");

            return authResponse;
        }

        private async Task<AuthResponse> BuildJwtResponseAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var (token, exp, role) = _jwt.CreateToken(user, roles);
            return new AuthResponse
            {
                AccessToken = token,
                ExpiresAtUtc = exp,
                RequiresMfa = false,
                User = new AuthClinicUserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    ClinicId = user.ClinicId?.ToString(),
                    ClinicName = user.Clinic?.Name,
                    Role = role
                }
            };
        }
    }
}
