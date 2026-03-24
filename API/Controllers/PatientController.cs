using ClinicOps.API.DTOs.Patient;
using ClinicOps.Application.Services.Patient;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ApplicationDbContext _db;

        public PatientController(IPatientService patientService, ApplicationDbContext db)
        {
            _patientService = patientService;
            _db = db;
        }

        /// <summary>
        /// Register a patient at reception and create a waiting case
        /// </summary>
        /// <param name="request">Patient registration details</param>
        /// <returns>Registered patient with case information</returns>
        /// <response code="200">Patient registered successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Forbidden - SuperAdmin cannot register patients</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientResponseDto>> RegisterPatient(
            [FromBody] RegisterPatientRequest request)
        {
            // Get clinic ID from JWT claims or request body
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            Guid clinicId;

            // Check if user is SuperAdmin (no clinicId in token)
            if (string.IsNullOrEmpty(clinicIdClaim))
            {
                // SuperAdmin: use clinicId from request body, or use default test clinic GUID
                if (request.ClinicId.HasValue)
                {
                    clinicId = request.ClinicId.Value;
                }
                else
                {
                    // Use default test clinic GUID for SuperAdmin: 11111111-1111-1111-1111-111111111111
                    clinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                    
                    // Verify clinic exists, if not create it
                    var defaultClinic = await _db.Clinics.FindAsync(clinicId);
                    if (defaultClinic == null)
                    {
                        defaultClinic = new Clinic
                        {
                            Id = clinicId,
                            Name = "Default Test Clinic",
                            Address = "123 Test Street",
                            Phone = "+1234567890",
                            ClinicMode = ClinicMode.FullTeam,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _db.Clinics.Add(defaultClinic);
                        await _db.SaveChangesAsync();
                    }
                }
            }
            else
            {
                // Clinic user: use clinicId from token
                if (!Guid.TryParse(clinicIdClaim, out clinicId))
                {
                    return BadRequest("Invalid clinic ID in token.");
                }
            }

            try
            {
                var result = await _patientService.RegisterPatientAtReceptionAsync(
                    clinicId,
                    request);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all patients
        /// </summary>
        /// <param name="clinicId">Optional clinic ID filter (SuperAdmin only)</param>
        /// <returns>List of patients</returns>
        /// <response code="200">Returns list of patients</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<PatientResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<PatientResponseDto>>> GetAllPatients(
            [FromQuery] Guid? clinicId = null)
        {
            // Get clinic ID from JWT claims
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            Guid? filterClinicId = null;

            // Check if user is SuperAdmin (no clinicId in token)
            if (string.IsNullOrEmpty(clinicIdClaim))
            {
                // SuperAdmin: can filter by clinicId query parameter, or see all patients
                filterClinicId = clinicId;
            }
            else
            {
                // Clinic user: only see their clinic's patients
                if (!Guid.TryParse(clinicIdClaim, out var userClinicId))
                {
                    return BadRequest("Invalid clinic ID in token.");
                }
                filterClinicId = userClinicId;
            }

            // Query patients
            var query = _db.Patients
                .Include(p => p.Clinic)
                .Where(p => p.IsActive);

            // Apply clinic filter if specified
            if (filterClinicId.HasValue)
            {
                query = query.Where(p => p.ClinicId == filterClinicId.Value);
            }

            var patients = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Get latest patient case for each patient
            var patientIds = patients.Select(p => p.Id).ToList();
            var latestCases = await _db.PatientCases
                .Where(pc => patientIds.Contains(pc.PatientId))
                .GroupBy(pc => pc.PatientId)
                .Select(g => g.OrderByDescending(pc => pc.CreatedAt).First())
                .ToListAsync();

            // Map to DTOs
            var result = patients.Select(p =>
            {
                var latestCase = latestCases.FirstOrDefault(c => c.PatientId == p.Id);
                return new PatientResponseDto
                {
                    Id = p.Id,
                    ClinicId = p.ClinicId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    Phone = p.Phone,
                    CreatedAt = p.CreatedAt,
                    IsActive = p.IsActive,
                    PatientCaseId = latestCase?.Id,
                    PatientCaseStatus = latestCase?.Status.ToString()
                };
            }).ToList();

            return Ok(result);
        }
    }
}
