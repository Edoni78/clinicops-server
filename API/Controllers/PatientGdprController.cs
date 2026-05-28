using ClinicOps.API.DTOs.Gdpr;
using ClinicOps.Application.Services.Gdpr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:guid}")]
    [Authorize]
    public class PatientGdprController : ControllerBase
    {
        private readonly IPatientGdprService _gdprService;
        private readonly IAuditLogService _auditLogService;

        public PatientGdprController(IPatientGdprService gdprService, IAuditLogService auditLogService)
        {
            _gdprService = gdprService;
            _auditLogService = auditLogService;
        }

        [HttpGet("gdpr/export")]
        [ProducesResponseType(typeof(PatientGdprExportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientGdprExportDto>> Export(Guid patientId)
        {
            var clinicId = ResolveClinicId();
            if (!clinicId.HasValue)
                return BadRequest("Clinic context is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

            try
            {
                var data = await _gdprService.ExportPatientDataAsync(patientId, clinicId.Value, userId);
                await _auditLogService.TryLogAsync(
                    action: "PatientExported",
                    entityName: "Patient",
                    entityId: patientId.ToString(),
                    clinicId: clinicId.Value,
                    userId: userId,
                    status: "Success",
                    severity: "Security",
                    description: "Admin exported patient data.");
                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                await _auditLogService.TryLogAsync(
                    action: "PatientExported",
                    entityName: "Patient",
                    entityId: patientId.ToString(),
                    clinicId: clinicId.Value,
                    userId: userId,
                    status: "Failed",
                    severity: "Warning",
                    description: $"Patient export failed: {ex.Message}");
                return NotFound(ex.Message);
            }
        }

        [HttpPost("gdpr/anonymize")]
        [Authorize(Roles = "ClinicAdmin,Doctor,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Anonymize(Guid patientId)
        {
            var clinicId = ResolveClinicId();
            if (!clinicId.HasValue)
                return BadRequest("Clinic context is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

            try
            {
                var changed = await _gdprService.AnonymizePatientAsync(patientId, clinicId.Value);
                if (changed)
                    await _auditLogService.TryLogAsync(
                        action: "PatientAnonymized",
                        entityName: "Patient",
                        entityId: patientId.ToString(),
                        clinicId: clinicId.Value,
                        userId: userId,
                        status: "Success",
                        severity: "Critical",
                        description: "Patient personal identifiers were anonymized.");

                return Ok(new { patientId, anonymized = true });
            }
            catch (InvalidOperationException ex)
            {
                await _auditLogService.TryLogAsync(
                    action: "PatientAnonymized",
                    entityName: "Patient",
                    entityId: patientId.ToString(),
                    clinicId: clinicId.Value,
                    userId: userId,
                    status: "Failed",
                    severity: "Warning",
                    description: $"Patient anonymization failed: {ex.Message}");
                return NotFound(ex.Message);
            }
        }

        [HttpGet("consent")]
        [ProducesResponseType(typeof(PatientConsentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetConsent(Guid patientId)
        {
            var clinicId = ResolveClinicId();
            if (!clinicId.HasValue)
                return BadRequest("Clinic context is required.");

            try
            {
                var consent = await _gdprService.GetLatestConsentAsync(patientId, clinicId.Value);
                if (consent == null)
                    return NoContent();

                return Ok(consent);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("consent")]
        [Authorize(Roles = "ClinicAdmin,Doctor,Nurse,SuperAdmin")]
        [ProducesResponseType(typeof(PatientConsentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddConsent(Guid patientId, [FromBody] UpsertPatientConsentRequest request)
        {
            var clinicId = ResolveClinicId();
            if (!clinicId.HasValue)
                return BadRequest("Clinic context is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

            try
            {
                var consent = await _gdprService.AddConsentAsync(patientId, clinicId.Value, userId, request);
                await _auditLogService.TryLogAsync("MedicalRecordUpdated", "PatientConsent", patientId.ToString(), clinicId.Value, userId);
                return Ok(consent);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("consent/withdraw")]
        [Authorize(Roles = "ClinicAdmin,Doctor,Nurse,SuperAdmin")]
        [ProducesResponseType(typeof(PatientConsentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> WithdrawConsent(Guid patientId, [FromBody] WithdrawPatientConsentRequest request)
        {
            var clinicId = ResolveClinicId();
            if (!clinicId.HasValue)
                return BadRequest("Clinic context is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

            try
            {
                var consent = await _gdprService.WithdrawConsentAsync(patientId, clinicId.Value, userId, request);
                await _auditLogService.TryLogAsync("MedicalRecordUpdated", "PatientConsent", patientId.ToString(), clinicId.Value, userId);
                return Ok(consent);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        private Guid? ResolveClinicId()
        {
            if (User.IsInRole("SuperAdmin"))
            {
                var clinicFromQuery = Request.Query["clinicId"].FirstOrDefault();
                if (Guid.TryParse(clinicFromQuery, out var queryClinicId))
                    return queryClinicId;

                return null;
            }

            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            if (Guid.TryParse(clinicIdClaim, out var clinicId))
                return clinicId;

            return null;
        }
    }
}
