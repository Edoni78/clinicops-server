using ClinicOps.API.DTOs.Auth;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Auth
{
    public class ClinicRegistrationService : IClinicRegistrationService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClinicRegistrationService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task ApplyForClinicAsync(RegisterClinicRequest request)
        {
            if (!Enum.IsDefined(typeof(ClinicMode), request.ClinicMode))
                throw new InvalidOperationException("clinicMode is required and must be either SoloDoctor or FullTeam.");

            var existsUser = await _userManager.FindByEmailAsync(request.Email);
            if (existsUser != null)
                throw new InvalidOperationException("Email already in use.");

            var hasPending = await _db.ClinicApplications.AnyAsync(a =>
                a.AdminEmail == request.Email &&
                a.Status == ApplicationStatus.Pending);

            if (hasPending)
                throw new InvalidOperationException("You already have a pending application.");

            var passwordHash = _userManager.PasswordHasher.HashPassword(null!, request.Password);

            var app = new ClinicApplication
            {
                ClinicName = request.ClinicName,
                AdminEmail = request.Email,
                AdminPasswordHash = passwordHash,
                ClinicMode = request.ClinicMode
            };

            _db.ClinicApplications.Add(app);
            await _db.SaveChangesAsync();
        }
    }
}
