using ClinicOps.API.DTOs.Clinic;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClinicController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ClinicController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        /// <summary>
        /// Get the logged-in clinic's profile (card: name, logo, address, phone, description).
        /// Only clinic users (with clinicId in token) can call this; SuperAdmin returns 400.
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ClinicProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClinicProfileDto>> GetProfile()
        {
            var clinicId = GetClinicIdFromToken();
            if (!clinicId.HasValue)
                return BadRequest("Only clinic users can access clinic profile. Login with a clinic account.");

            var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clinicId.Value);
            if (clinic == null)
                return NotFound("Clinic not found.");

            return Ok(new ClinicProfileDto
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                LogoUrl = clinic.LogoUrl,
                Description = clinic.Description,
                CreatedAt = clinic.CreatedAt,
                IsActive = clinic.IsActive
            });
        }

        /// <summary>
        /// Update the logged-in clinic's profile (name, address, phone, logo URL, description).
        /// Only clinic users (e.g. ClinicAdmin) can update.
        /// </summary>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ClinicProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClinicProfileDto>> UpdateProfile([FromBody] UpdateClinicProfileRequest request)
        {
            var clinicId = GetClinicIdFromToken();
            if (!clinicId.HasValue)
                return BadRequest("Only clinic users can update clinic profile.");

            var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId.Value);
            if (clinic == null)
                return NotFound("Clinic not found.");

            if (request.Name != null) clinic.Name = request.Name;
            if (request.Address != null) clinic.Address = request.Address;
            if (request.Phone != null) clinic.Phone = request.Phone;
            if (request.LogoUrl != null) clinic.LogoUrl = request.LogoUrl;
            if (request.Description != null) clinic.Description = request.Description;

            await _db.SaveChangesAsync();

            return Ok(new ClinicProfileDto
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                LogoUrl = clinic.LogoUrl,
                Description = clinic.Description,
                CreatedAt = clinic.CreatedAt,
                IsActive = clinic.IsActive
            });
        }

        /// <summary>
        /// Upload a logo for the logged-in clinic. Saves file under wwwroot/uploads/clinics/{clinicId}/ and sets LogoUrl.
        /// </summary>
        [HttpPost("profile/logo")]
        [ProducesResponseType(typeof(ClinicProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClinicProfileDto>> UploadLogo(IFormFile? file)
        {
            var clinicId = GetClinicIdFromToken();
            if (!clinicId.HasValue)
                return BadRequest("Only clinic users can upload clinic logo.");

            var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId.Value);
            if (clinic == null)
                return NotFound("Clinic not found.");

            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                return BadRequest("Allowed formats: " + string.Join(", ", allowed));

            var uploadsDir = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads", "clinics", clinicId.Value.ToString());
            Directory.CreateDirectory(uploadsDir);
            var fileName = "logo" + ext;
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var logoUrl = $"/uploads/clinics/{clinicId.Value}/{fileName}";
            clinic.LogoUrl = logoUrl;
            await _db.SaveChangesAsync();

            return Ok(new ClinicProfileDto
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                LogoUrl = clinic.LogoUrl,
                Description = clinic.Description,
                CreatedAt = clinic.CreatedAt,
                IsActive = clinic.IsActive
            });
        }

        private Guid? GetClinicIdFromToken()
        {
            var claim = User.FindFirst("clinicId")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}
