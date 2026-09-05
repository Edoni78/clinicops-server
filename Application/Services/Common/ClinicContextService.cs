using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Common
{
    public class ClinicContextService : IClinicContextService
    {
        public static readonly Guid DefaultClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly ApplicationDbContext _db;

        public ClinicContextService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync(ClaimsPrincipal user)
        {
            var fromToken = GetClinicIdFromToken(user);
            if (fromToken.HasValue)
                return (false, fromToken.Value);

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

        public async Task<(bool isSuperAdmin, Guid? clinicId)> TryResolveClinicIdAsync(
            ClaimsPrincipal user,
            Guid? fromQuery = null)
        {
            var fromToken = GetClinicIdFromToken(user);
            if (fromToken.HasValue)
                return (false, fromToken.Value);

            if (fromQuery.HasValue)
                return (true, fromQuery.Value);

            var clinic = await _db.Clinics.FindAsync(DefaultClinicId);
            if (clinic != null)
                return (true, DefaultClinicId);

            return (true, null);
        }

        public Guid? GetClinicIdFromToken(ClaimsPrincipal user)
        {
            var claim = user.FindFirst("clinicId")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        public Guid? ResolveClinicIdForPrivacy(ClaimsPrincipal user, Guid? fromQuery = null)
        {
            if (user.IsInRole("SuperAdmin"))
                return fromQuery;

            return GetClinicIdFromToken(user);
        }
    }
}
