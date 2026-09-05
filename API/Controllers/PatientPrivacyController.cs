using ClinicOps.API.DTOs.Privacy;
using ClinicOps.Application.Services.Audit;
using ClinicOps.Application.Services.Common;
using ClinicOps.Application.Services.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:guid}")]
    [Authorize]
    public class PatientPrivacyController : ControllerBase
    {
        private readonly IPatientPrivacyService _privacyService;
        private readonly IAuditLogService _auditLogService;
        private readonly IClinicContextService _clinicContextService;

        public PatientPrivacyController(
            IPatientPrivacyService privacyService,
            IAuditLogService auditLogService,
            IClinicContextService clinicContextService)
        {
            _privacyService = privacyService;
            _auditLogService = auditLogService;
            _clinicContextService = clinicContextService;
        }

        // Route kept as gdpr/* for existing clients.
        [HttpGet("gdpr/export")]
        [ProducesResponseType(typeof(PatientPrivacyExportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientPrivacyExportDto>> Export(Guid patientId, [FromQuery] Guid? clinicId = null)
        {
            var resolvedClinicId = ResolveClinicId(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("Clinic context is required.");

            var userId = User.GetUserId();

            try
            {
                var data = await _privacyService.ExportPatientDataAsync(patientId, resolvedClinicId.Value, userId);
                await _auditLogService.TryLogAsync(
                    action: "PatientExported",
                    entityName: "Patient",
                    entityId: patientId.ToString(),
                    clinicId: resolvedClinicId.Value,
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
                    clinicId: resolvedClinicId.Value,
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
        public async Task<IActionResult> Anonymize(Guid patientId, [FromQuery] Guid? clinicId = null)
        {
            var resolvedClinicId = ResolveClinicId(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("Clinic context is required.");

            var userId = User.GetUserId();

            try
            {
                var changed = await _privacyService.AnonymizePatientAsync(patientId, resolvedClinicId.Value);
                if (changed)
                    await _auditLogService.TryLogAsync(
                        action: "PatientAnonymized",
                        entityName: "Patient",
                        entityId: patientId.ToString(),
                        clinicId: resolvedClinicId.Value,
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
                    clinicId: resolvedClinicId.Value,
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
        public async Task<IActionResult> GetConsent(Guid patientId, [FromQuery] Guid? clinicId = null)
        {
            var resolvedClinicId = ResolveClinicId(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("Clinic context is required.");

            try
            {
                var consent = await _privacyService.GetLatestConsentAsync(patientId, resolvedClinicId.Value);
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
        public async Task<IActionResult> AddConsent(
            Guid patientId,
            [FromBody] UpsertPatientConsentRequest request,
            [FromQuery] Guid? clinicId = null)
        {
            var resolvedClinicId = ResolveClinicId(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("Clinic context is required.");

            var userId = User.GetUserId();

            try
            {
                var consent = await _privacyService.AddConsentAsync(patientId, resolvedClinicId.Value, userId, request);
                await _auditLogService.TryLogAsync("MedicalRecordUpdated", "PatientConsent", patientId.ToString(), resolvedClinicId.Value, userId);
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
        public async Task<IActionResult> WithdrawConsent(
            Guid patientId,
            [FromBody] WithdrawPatientConsentRequest request,
            [FromQuery] Guid? clinicId = null)
        {
            var resolvedClinicId = ResolveClinicId(clinicId);
            if (!resolvedClinicId.HasValue)
                return BadRequest("Clinic context is required.");

            var userId = User.GetUserId();

            try
            {
                var consent = await _privacyService.WithdrawConsentAsync(patientId, resolvedClinicId.Value, userId, request);
                await _auditLogService.TryLogAsync("MedicalRecordUpdated", "PatientConsent", patientId.ToString(), resolvedClinicId.Value, userId);
                return Ok(consent);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        private Guid? ResolveClinicId(Guid? clinicIdQuery) =>
            _clinicContextService.ResolveClinicIdForPrivacy(User, clinicIdQuery);
    }
}
