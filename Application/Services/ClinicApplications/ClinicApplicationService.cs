using ClinicOps.API.DTOs.ClinicApplication;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.ClinicApplications
{
    public class ClinicApplicationService : IClinicApplicationService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClinicApplicationService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<List<ClinicApplicationDto>> ListAsync(string? status = null)
        {
            var query = _db.ClinicApplications.AsNoTracking();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var statusEnum))
                query = query.Where(a => a.Status == statusEnum);

            return await query
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => new ClinicApplicationDto
                {
                    Id = a.Id,
                    ClinicName = a.ClinicName,
                    AdminEmail = a.AdminEmail,
                    Status = a.Status,
                    ClinicMode = a.ClinicMode,
                    CreatedAtUtc = a.CreatedAtUtc,
                    ReviewedAtUtc = a.ReviewedAtUtc,
                    ReviewNote = a.ReviewNote
                })
                .ToListAsync();
        }

        public async Task<ClinicApplicationApproveResult> ApproveAsync(int id, string? reviewNote = null)
        {
            var app = await _db.ClinicApplications.FirstOrDefaultAsync(a => a.Id == id);
            if (app == null)
                throw new KeyNotFoundException("Application not found.");

            if (!app.CanBeReviewed())
                throw new InvalidOperationException(
                    $"Application is already {app.Status}. Only pending applications can be approved.");

            var existingUser = await _userManager.FindByEmailAsync(app.AdminEmail);
            if (existingUser != null)
                throw new InvalidOperationException("A user with this email already exists. Cannot approve.");

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var clinic = new Clinic
                {
                    Name = app.ClinicName,
                    ClinicMode = app.ClinicMode,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _db.Clinics.Add(clinic);
                await _db.SaveChangesAsync();

                var adminUser = new ApplicationUser
                {
                    UserName = app.AdminEmail,
                    Email = app.AdminEmail,
                    NormalizedUserName = app.AdminEmail.ToUpperInvariant(),
                    NormalizedEmail = app.AdminEmail.ToUpperInvariant(),
                    EmailConfirmed = true,
                    ClinicId = clinic.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    PasswordHash = app.AdminPasswordHash
                };

                var createResult = await _userManager.CreateAsync(adminUser);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

                await _userManager.AddToRoleAsync(adminUser, "ClinicAdmin");

                app.Approve(reviewNote);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ClinicApplicationApproveResult
                {
                    ClinicId = clinic.Id,
                    AdminUserId = adminUser.Id,
                    Message = "Application approved. Clinic and admin user created. They can now login with the email and password they used when applying."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RejectAsync(int id, string? reviewNote = null)
        {
            var app = await _db.ClinicApplications.FirstOrDefaultAsync(a => a.Id == id);
            if (app == null)
                throw new KeyNotFoundException("Application not found.");

            app.Reject(reviewNote);
            await _db.SaveChangesAsync();
        }
    }
}
