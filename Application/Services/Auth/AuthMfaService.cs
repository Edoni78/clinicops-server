using ClinicOps.API.DTOs.Auth;
using ClinicOps.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace ClinicOps.Application.Services.Auth
{
    public class AuthMfaService : IAuthMfaService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthMfaService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<MfaSetupResponse> GenerateSetupAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrWhiteSpace(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Failed to generate MFA key.");

            var appName = "ClinicOps";
            var email = user.Email ?? user.UserName ?? user.Id;
            var encodedIssuer = Uri.EscapeDataString(appName);
            var encodedEmail = Uri.EscapeDataString(email);
            var encodedSecret = Uri.EscapeDataString(key);
            var uri = $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={encodedSecret}&issuer={encodedIssuer}&digits=6";

            return new MfaSetupResponse
            {
                SharedKey = FormatKey(key),
                ManualEntryKey = key,
                QrCodeUri = uri
            };
        }

        public async Task<MfaEnabledResponse> EnableAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var normalizedCode = NormalizeCode(code);
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                normalizedCode);

            if (!isValid)
                throw new InvalidOperationException("Invalid MFA code.");

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return new MfaEnabledResponse
            {
                Enabled = true,
                RecoveryCodes = recoveryCodes?.ToList() ?? new List<string>()
            };
        }

        private static string NormalizeCode(string code) => code.Replace(" ", "").Replace("-", "");

        private static string FormatKey(string key)
        {
            var result = new StringBuilder();
            var currentPosition = 0;
            while (currentPosition + 4 < key.Length)
            {
                result.Append(key.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }

            if (currentPosition < key.Length)
                result.Append(key.AsSpan(currentPosition));

            return result.ToString().ToLowerInvariant();
        }
    }
}
