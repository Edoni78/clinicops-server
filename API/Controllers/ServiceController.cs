using ClinicOps.API.DTOs.Service;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ServiceController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// List services for the clinic. Clinic users see their clinic's services; SuperAdmin can pass clinicId query.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ServiceDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ServiceDto>>> List([FromQuery] Guid? clinicId = null)
        {
            var (_, resolvedClinicId) = await ResolveClinicIdAsync(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("ClinicId required for SuperAdmin, or login as a clinic user.");

            var list = await _db.Services
                .Where(s => s.ClinicId == resolvedClinicId.Value && s.IsActive)
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
            return Ok(list);
        }

        /// <summary>
        /// Get a single service by id.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceDto>> GetById(Guid id, [FromQuery] Guid? clinicId = null)
        {
            var (_, resolvedClinicId) = await ResolveClinicIdAsync(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("ClinicId required for SuperAdmin, or login as a clinic user.");

            var service = await _db.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.ClinicId == resolvedClinicId.Value);
            if (service == null)
                return NotFound("Service not found.");

            return Ok(new ServiceDto
            {
                Id = service.Id,
                ClinicId = service.ClinicId,
                Name = service.Name,
                Price = service.Price,
                CreatedAt = service.CreatedAt,
                IsActive = service.IsActive
            });
        }

        /// <summary>
        /// Create a new service (name and price) for the clinic.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ServiceDto>> Create([FromBody] CreateServiceRequest request, [FromQuery] Guid? clinicId = null)
        {
            var (_, resolvedClinicId) = await ResolveClinicIdAsync(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("ClinicId required for SuperAdmin, or login as a clinic user.");

            var clinic = await _db.Clinics.FindAsync(resolvedClinicId.Value);
            if (clinic == null)
                return BadRequest("Clinic not found.");

            var service = new Service
            {
                ClinicId = resolvedClinicId.Value,
                Name = request.Name.Trim(),
                Price = request.Price,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = service.Id }, new ServiceDto
            {
                Id = service.Id,
                ClinicId = service.ClinicId,
                Name = service.Name,
                Price = service.Price,
                CreatedAt = service.CreatedAt,
                IsActive = service.IsActive
            });
        }

        /// <summary>
        /// Update a service (name and/or price).
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceDto>> Update(Guid id, [FromBody] UpdateServiceRequest request, [FromQuery] Guid? clinicId = null)
        {
            var (_, resolvedClinicId) = await ResolveClinicIdAsync(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("ClinicId required for SuperAdmin, or login as a clinic user.");

            var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id && s.ClinicId == resolvedClinicId.Value);
            if (service == null)
                return NotFound("Service not found.");

            if (request.Name != null)
                service.Name = request.Name.Trim();
            if (request.Price.HasValue)
                service.Price = request.Price.Value;

            await _db.SaveChangesAsync();

            return Ok(new ServiceDto
            {
                Id = service.Id,
                ClinicId = service.ClinicId,
                Name = service.Name,
                Price = service.Price,
                CreatedAt = service.CreatedAt,
                IsActive = service.IsActive
            });
        }

        /// <summary>
        /// Delete (soft-deactivate) a service. Service is set IsActive = false so it no longer appears in list.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? clinicId = null)
        {
            var (_, resolvedClinicId) = await ResolveClinicIdAsync(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("ClinicId required for SuperAdmin, or login as a clinic user.");

            var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id && s.ClinicId == resolvedClinicId.Value);
            if (service == null)
                return NotFound("Service not found.");

            service.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private async Task<(bool isSuperAdmin, Guid? clinicId)> ResolveClinicIdAsync(Guid? fromQuery = null)
        {
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            if (!string.IsNullOrEmpty(clinicIdClaim) && Guid.TryParse(clinicIdClaim, out var fromToken))
                return (false, fromToken);
            if (fromQuery.HasValue)
                return (true, fromQuery.Value);
            var defaultId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var clinic = await _db.Clinics.FindAsync(defaultId);
            if (clinic != null)
                return (true, defaultId);
            return (true, null);
        }
    }
}
