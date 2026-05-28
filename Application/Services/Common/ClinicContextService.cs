using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Common
{
    public class ClinicContextService : IClinicContextService
    {
        private readonly ApplicationDbContext _db;
        private static readonly Guid DefaultClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public ClinicContextService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync(ClaimsPrincipal user)
        {
            var clinicIdClaim = user.FindFirst("clinicId")?.Value;
            if (!string.IsNullOrEmpty(clinicIdClaim) && Guid.TryParse(clinicIdClaim, out var fromToken))
                return (false, fromToken);

            var clinic = await _db.Clinics.FindAsync(DefaultClinicId);
            if (clinic == null)
            {
                clinic = new Clinic
                {
                    Id = DefaultClinicId,
                    Name = "Default Test Clinic",
                    Address = "123 Test Street",
                    Phone = "+1234567890",
                    ClinicMode = ClinicMode.FullTeam,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _db.Clinics.Add(clinic);
                await _db.SaveChangesAsync();
            }

            return (true, DefaultClinicId);
        }
    }
}
