using ClinicOps.API.DTOs.DoctorProfile;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor")]
    public class DoctorProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public DoctorProfileController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        /// <summary>
        /// Get the logged-in doctor's profile (display name, signature URL, stamp URL).
        /// Only users in role Doctor can call this.
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorProfileDto>> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            return Ok(new DoctorProfileDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DoctorDisplayName ?? user.Email,
                SignatureUrl = user.SignatureUrl,
                StampUrl = user.StampUrl
            });
        }

        /// <summary>
        /// Update the logged-in doctor's display name.
        /// </summary>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorProfileDto>> UpdateProfile([FromBody] UpdateDoctorProfileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            if (request.DisplayName != null)
                user.DoctorDisplayName = request.DisplayName.Trim().Length > 0 ? request.DisplayName.Trim() : null;

            await _db.SaveChangesAsync();

            return Ok(new DoctorProfileDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DoctorDisplayName ?? user.Email,
                SignatureUrl = user.SignatureUrl,
                StampUrl = user.StampUrl
            });
        }

        /// <summary>
        /// Upload the doctor's signature image. Saves under wwwroot/uploads/doctors/{userId}/signature.{ext}.
        /// </summary>
        [HttpPost("profile/signature")]
        [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorProfileDto>> UploadSignature(IFormFile? file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                return BadRequest("Allowed formats: " + string.Join(", ", allowed));

            var uploadsDir = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads", "doctors", userId);
            Directory.CreateDirectory(uploadsDir);
            var fileName = "signature" + ext;
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            user.SignatureUrl = $"/uploads/doctors/{userId}/{fileName}";
            await _db.SaveChangesAsync();

            return Ok(new DoctorProfileDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DoctorDisplayName ?? user.Email,
                SignatureUrl = user.SignatureUrl,
                StampUrl = user.StampUrl
            });
        }

        /// <summary>
        /// Upload the doctor's stamp image. Saves under wwwroot/uploads/doctors/{userId}/stamp.{ext}.
        /// </summary>
        [HttpPost("profile/stamp")]
        [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorProfileDto>> UploadStamp(IFormFile? file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                return BadRequest("Allowed formats: " + string.Join(", ", allowed));

            var uploadsDir = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads", "doctors", userId);
            Directory.CreateDirectory(uploadsDir);
            var fileName = "stamp" + ext;
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            user.StampUrl = $"/uploads/doctors/{userId}/{fileName}";
            await _db.SaveChangesAsync();

            return Ok(new DoctorProfileDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DoctorDisplayName ?? user.Email,
                SignatureUrl = user.SignatureUrl,
                StampUrl = user.StampUrl
            });
        }
    }
}
