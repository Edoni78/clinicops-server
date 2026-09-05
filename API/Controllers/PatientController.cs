using ClinicOps.API.DTOs.Patient;
using ClinicOps.Application.Services.Common;
using ClinicOps.Application.Services.Audit;
using ClinicOps.Application.Services.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IAuditLogService _auditLogService;

        public PatientController(
            IPatientService patientService,
            IPatientQueryService patientQueryService,
            IClinicContextService clinicContextService,
            IAuditLogService auditLogService)
        {
            _patientService = patientService;
            _patientQueryService = patientQueryService;
            _clinicContextService = clinicContextService;
            _auditLogService = auditLogService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientResponseDto>> RegisterPatient(
            [FromBody] RegisterPatientRequest request)
        {
            Guid clinicId;
            try
            {
                clinicId = await ResolveRegistrationClinicIdAsync(request.ClinicId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            try
            {
                var result = await _patientService.RegisterPatientAtReceptionAsync(clinicId, request);
                await _auditLogService.TryLogAsync(
                    "PatientCreated",
                    "Patient",
                    result.Id.ToString(),
                    clinicId,
                    User.GetUserId());

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

        [HttpPost("{id:guid}/open-case")]
        [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientResponseDto>> OpenCaseForPatient(
            Guid id,
            [FromBody] OpenPatientCaseRequest request)
        {
            Guid clinicId;
            try
            {
                clinicId = await ResolveRegistrationClinicIdAsync(request.ClinicId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            try
            {
                var result = await _patientService.OpenCaseForExistingPatientAsync(clinicId, id, request);
                await _auditLogService.TryLogAsync(
                    "PatientCaseOpened",
                    "PatientCase",
                    result.PatientCaseId?.ToString(),
                    clinicId,
                    User.GetUserId());

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<PatientResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<PatientResponseDto>>> GetAllPatients(
            [FromQuery] Guid? clinicId = null)
        {
            try
            {
                return Ok(await _patientQueryService.GetAllPatientsAsync(clinicId, User));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id:guid}/emr")]
        [ProducesResponseType(typeof(PatientEmrDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientEmrDto>> GetPatientEmr(Guid id, [FromQuery] bool doctorView = false)
        {
            try
            {
                return Ok(await _patientQueryService.GetPatientEmrAsync(id, doctorView, User));
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

        /// <summary>
        /// Preserves prior registration clinic resolution:
        /// token clinicId if present; else request body clinicId; else default via ClinicContextService.
        /// </summary>
        private async Task<Guid> ResolveRegistrationClinicIdAsync(Guid? requestClinicId)
        {
            var fromToken = _clinicContextService.GetClinicIdFromToken(User);
            if (fromToken.HasValue)
                return fromToken.Value;

            if (requestClinicId.HasValue)
                return requestClinicId.Value;

            var (_, clinicId) = await _clinicContextService.ResolveClinicIdAsync(User);
            return clinicId;
        }
    }
}
