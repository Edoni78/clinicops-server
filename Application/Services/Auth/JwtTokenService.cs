using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using ClinicOps.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicOps.Application.Services.Auth
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public (string token, DateTime expiresAtUtc, string? role) CreateToken(
            ApplicationUser user,
            IList<string> roles)
        {
            var issuer = _config["Jwt:Issuer"]!;
            var audience = _config["Jwt:Audience"]!;
            var key = _config["Jwt:Key"]!;
            var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"]!);

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);

            // ==========================
            // BASE CLAIMS
            // ==========================
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new(ClaimTypes.NameIdentifier, user.Id)
            };

            // ==========================
            // ROLE CLAIMS
            // ==========================
            foreach (var r in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }

            // 👉 Primary role (frontend-friendly)
            var primaryRole = roles.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(primaryRole))
            {
                claims.Add(new Claim("primaryRole", primaryRole));
            }

            // ==========================
            // TENANT CLAIMS (CLINIC USERS)
            // ==========================
            if (user.ClinicId.HasValue)
            {
                claims.Add(new Claim("clinicId", user.ClinicId.Value.ToString()));

                if (user.Clinic != null)
                {
                    claims.Add(new Claim("clinicName", user.Clinic.Name));
                }
            }

            // ==========================
            // TOKEN CREATION
            // ==========================
            var signingKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var creds =
                new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: creds
            );

            return (
                new JwtSecurityTokenHandler().WriteToken(token),
                expiresAtUtc,
                primaryRole
            );
        }
    }
}
