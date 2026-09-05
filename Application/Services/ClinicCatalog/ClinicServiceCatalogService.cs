using ClinicOps.API.DTOs.Service;
using ClinicOps.Application.Services.Common;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.Application.Services.ClinicCatalog
{
    public class ClinicServiceCatalogService : IClinicServiceCatalogService
    {
        private readonly ApplicationDbContext _db;
        private readonly IClinicContextService _clinicContext;

        public ClinicServiceCatalogService(ApplicationDbContext db, IClinicContextService clinicContext)
        {
            _db = db;
            _clinicContext = clinicContext;
        }

        public async Task<List<ServiceDto>> ListAsync(ClaimsPrincipal user, Guid? clinicIdQuery = null)
        {
            var clinicId = await RequireClinicIdAsync(user, clinicIdQuery);

            return await _db.Services
                .AsNoTracking()
                .Where(s => s.ClinicId == clinicId && s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    ClinicId = s.ClinicId,
                    Name = s.Name,
                    Price = s.Price,
                    CreatedAt = s.CreatedAt,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<ServiceDto> GetByIdAsync(Guid id, ClaimsPrincipal user, Guid? clinicIdQuery = null)
        {
            var clinicId = await RequireClinicIdAsync(user, clinicIdQuery);

            var service = await _db.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.ClinicId == clinicId);

            if (service == null)
                throw new KeyNotFoundException("Service not found.");

            return MapDto(service);
        }

        public async Task<ServiceDto> CreateAsync(
            CreateServiceRequest request,
            ClaimsPrincipal user,
            Guid? clinicIdQuery = null)
        {
            var clinicId = await RequireClinicIdAsync(user, clinicIdQuery);

            var clinic = await _db.Clinics.FindAsync(clinicId);
            if (clinic == null)
                throw new InvalidOperationException("Clinic not found.");

            var service = new Service
            {
                ClinicId = clinicId,
                Name = request.Name.Trim(),
                Price = request.Price,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            return MapDto(service);
        }

        public async Task<ServiceDto> UpdateAsync(
            Guid id,
            UpdateServiceRequest request,
            ClaimsPrincipal user,
            Guid? clinicIdQuery = null)
        {
            var clinicId = await RequireClinicIdAsync(user, clinicIdQuery);

            var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id && s.ClinicId == clinicId);
            if (service == null)
                throw new KeyNotFoundException("Service not found.");

            service.UpdateDetails(request.Name, request.Price);

            await _db.SaveChangesAsync();
            return MapDto(service);
        }

        public async Task DeleteAsync(Guid id, ClaimsPrincipal user, Guid? clinicIdQuery = null)
        {
            var clinicId = await RequireClinicIdAsync(user, clinicIdQuery);

            var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id && s.ClinicId == clinicId);
            if (service == null)
                throw new KeyNotFoundException("Service not found.");

            service.Deactivate();
            await _db.SaveChangesAsync();
        }

        private async Task<Guid> RequireClinicIdAsync(ClaimsPrincipal user, Guid? clinicIdQuery)
        {
            var (_, clinicId) = await _clinicContext.TryResolveClinicIdAsync(user, clinicIdQuery);
            if (!clinicId.HasValue)
                throw new InvalidOperationException("ClinicId required for SuperAdmin, or login as a clinic user.");
            return clinicId.Value;
        }

        private static ServiceDto MapDto(Service s) => new()
        {
            Id = s.Id,
            ClinicId = s.ClinicId,
            Name = s.Name,
            Price = s.Price,
            CreatedAt = s.CreatedAt,
            IsActive = s.IsActive
        };
    }
}
