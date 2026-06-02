using ClinicOps.API.DTOs.Patient;
using ClinicOps.Application.Services.Common;
using ClinicOps.Application.Services.Gdpr;
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
        private readonly IPatientQueryService _patientQueryService;
        private readonly IClinicContextService _clinicContextService;
        private readonly ApplicationDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public PatientController(
            IPatientService patientService,
            IPatientQueryService patientQueryService,
            IClinicContextService clinicContextService,
            ApplicationDbContext db,
            IAuditLogService auditLogService)
        {
            _patientService = patientService;
            _patientQueryService = patientQueryService;
            _clinicContextService = clinicContextService;
            _db = db;
            _auditLogService = auditLogService;
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
            Guid clinicId;
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            if (string.IsNullOrEmpty(clinicIdClaim))
            {
                if (request.ClinicId.HasValue)
                {
                    clinicId = request.ClinicId.Value;
                }
                else
                {
                    (_, clinicId) = await _clinicContextService.ResolveClinicIdAsync(User);
                }
            }
            else
            {
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                await _auditLogService.TryLogAsync("PatientCreated", "Patient", result.Id.ToString(), clinicId, userId);

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
        /// Open a new waiting case for an existing patient (reception return visit).
        /// </summary>
        [HttpPost("{id:guid}/open-case")]
        [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientResponseDto>> OpenCaseForPatient(
            Guid id,
            [FromBody] OpenPatientCaseRequest request)
        {
            Guid clinicId;
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            if (string.IsNullOrEmpty(clinicIdClaim))
            {
                if (request.ClinicId.HasValue)
                    clinicId = request.ClinicId.Value;
                else
                    (_, clinicId) = await _clinicContextService.ResolveClinicIdAsync(User);
            }
            else
            {
                if (!Guid.TryParse(clinicIdClaim, out clinicId))
                    return BadRequest("Invalid clinic ID in token.");
            }

            try
            {
                var result = await _patientService.OpenCaseForExistingPatientAsync(
                    clinicId,
                    id,
                    request);
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
                await _auditLogService.TryLogAsync(
                    "PatientCaseOpened",
                    "PatientCase",
                    result.PatientCaseId?.ToString(),
                    clinicId,
                    userId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
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
            try
            {
                var result = await _patientQueryService.GetAllPatientsAsync(clinicId, User);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get EMR history for a specific patient, including consult dates, doctor, vitals, diagnosis, and therapy.
        /// </summary>
        [HttpGet("{id:guid}/emr")]
        [ProducesResponseType(typeof(PatientEmrDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientEmrDto>> GetPatientEmr(Guid id, [FromQuery] bool doctorView = false)
        {
            try
            {
                var result = await _patientQueryService.GetPatientEmrAsync(id, doctorView, User);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Clinic staff/SuperAdmin: soft delete patient (marks inactive).
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePatient(Guid id, [FromQuery] Guid? clinicId = null)
        {
            try
            {
                await _patientQueryService.DeletePatientAsync(id, clinicId, User);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
