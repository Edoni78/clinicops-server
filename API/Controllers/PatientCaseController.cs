using ClinicOps.API.DTOs.LabResult;
using ClinicOps.API.DTOs.MedicalReport;
using ClinicOps.API.DTOs.PatientCase;
using ClinicOps.API.DTOs.Vitals;
using ClinicOps.Application.Services.Common;
using ClinicOps.Application.Services.Patient;
using ClinicOps.Application.Services.Realtime;
using ClinicOps.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientCaseController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IClinicContextService _clinicContextService;
        private readonly IClinicRealtimeNotifier _realtimeNotifier;
        private readonly IPatientCaseReportService _patientCaseReportService;
        private readonly IPatientCaseWorkflowService _patientCaseWorkflowService;
        private readonly IPatientCaseQueryService _patientCaseQueryService;
        private readonly IPatientCaseCommandService _patientCaseCommandService;
        private readonly IPatientCaseLabService _patientCaseLabService;
        private readonly IPatientCasePdfFacadeService _patientCasePdfFacadeService;

        public PatientCaseController(
            IWebHostEnvironment env,
            IClinicContextService clinicContextService,
            IClinicRealtimeNotifier realtimeNotifier,
            IPatientCaseReportService patientCaseReportService,
            IPatientCaseWorkflowService patientCaseWorkflowService,
            IPatientCaseQueryService patientCaseQueryService,
            IPatientCaseCommandService patientCaseCommandService,
            IPatientCaseLabService patientCaseLabService,
            IPatientCasePdfFacadeService patientCasePdfFacadeService)
        {
            _env = env;
            _clinicContextService = clinicContextService;
            _realtimeNotifier = realtimeNotifier;
            _patientCaseReportService = patientCaseReportService;
            _patientCaseWorkflowService = patientCaseWorkflowService;
            _patientCaseQueryService = patientCaseQueryService;
            _patientCaseCommandService = patientCaseCommandService;
            _patientCaseLabService = patientCaseLabService;
            _patientCasePdfFacadeService = patientCasePdfFacadeService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<PatientCaseListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PatientCaseListItemDto>>> List([FromQuery] string? status = null)
        {
            return Ok(await _patientCaseQueryService.ListAsync(status, User));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PatientCaseDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientCaseDetailDto>> GetById(Guid id)
        {
            try
            {
                return Ok(await _patientCaseQueryService.GetByIdAsync(id, User));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
        }

        [HttpPost("{id:guid}/vitals")]
        [ProducesResponseType(typeof(VitalSignsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VitalSignsDto>> SubmitVitals(Guid id, [FromBody] SubmitVitalSignsRequest request)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            VitalSignsDto dto;
            try
            {
                dto = await _patientCaseCommandService.SubmitVitalsAsync(id, clinicId, request);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            await _realtimeNotifier.NotifyVitalsUpdatedAsync(clinicId, id, dto);
            return Ok(dto);
        }

        [HttpPatch("{id:guid}/protocol")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProtocolNumber(Guid id, [FromBody] UpdateProtocolNumberRequest request)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            try
            {
                var value = await _patientCaseCommandService.UpdateProtocolNumberAsync(
                    id, clinicId, request.ProtocolNumber, User);
                return Ok(new { id, protocolNumber = value });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id:guid}/report")]
        [Authorize(Roles = "Doctor,ClinicAdmin,SuperAdmin")]
        [ProducesResponseType(typeof(MedicalReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedicalReportDto>> SubmitReport(Guid id, [FromBody] SubmitMedicalReportRequest request)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var (_, clinicId) = await ResolveClinicIdAsync();
            MedicalReportDto dto;
            try
            {
                dto = await _patientCaseReportService.SubmitReportAsync(id, clinicId, userId, request);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            await _realtimeNotifier.NotifyReportUpdatedAsync(clinicId, id, dto);
            return Ok(dto);
        }

        [HttpGet("{id:guid}/report")]
        [Authorize(Roles = "Doctor,ClinicAdmin,SuperAdmin")]
        [ProducesResponseType(typeof(MedicalReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedicalReportDto>> GetReport(Guid id)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            var userId = User.GetUserId();
            try
            {
                return Ok(await _patientCaseReportService.GetReportAsync(id, clinicId, userId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id:guid}/report")]
        [Authorize(Roles = "Doctor,ClinicAdmin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            var userId = User.GetUserId();
            try
            {
                await _patientCaseReportService.DeleteReportAsync(id, clinicId, userId);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            await _realtimeNotifier.NotifyReportDeletedAsync(clinicId, id);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCase(Guid id, [FromQuery] Guid? clinicId = null)
        {
            Guid resolvedClinicId;
            try
            {
                resolvedClinicId = await _patientCaseWorkflowService.DeleteCaseAsync(id, clinicId, User);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            await _realtimeNotifier.NotifyCaseDeletedAsync(resolvedClinicId, id);
            return NoContent();
        }

        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
        {
            if (!PatientCaseStatusParser.TryParse(status, out var statusEnum))
                return BadRequest(PatientCaseStatusParser.AllowedStatusesMessage);

            var (_, clinicId) = await ResolveClinicIdAsync();
            try
            {
                statusEnum = await _patientCaseWorkflowService.UpdateStatusAsync(id, statusEnum, clinicId, User);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            await _realtimeNotifier.NotifyCaseStatusChangedAsync(clinicId, id, statusEnum.ToString());
            return Ok(new { id, status = statusEnum.ToString() });
        }

        [HttpPatch("{id:guid}/service")]
        [HttpPost("{id:guid}/service")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AttachService(Guid id, [FromQuery] Guid? serviceId, [FromBody] AttachServiceToCaseRequest? body)
        {
            var resolved = serviceId ?? body?.ServiceId;
            if (resolved == null || resolved == Guid.Empty)
                return BadRequest("serviceId is required (query string or JSON body).");

            var (_, clinicId) = await ResolveClinicIdAsync();
            try
            {
                var (attachedServiceId, attachedServiceName, attachedServicePrice) =
                    await _patientCaseCommandService.AttachServiceAsync(id, clinicId, resolved.Value);

                return Ok(new
                {
                    id,
                    serviceId = attachedServiceId,
                    serviceName = attachedServiceName,
                    servicePrice = attachedServicePrice
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id:guid}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadCaseReportPdf(Guid id)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            try
            {
                var (fileBytes, fileName) = await _patientCasePdfFacadeService
                    .GenerateCaseReportPdfAsync(id, clinicId, baseUrl, User.GetUserId());
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{id:guid}/labresults")]
        [ProducesResponseType(typeof(List<LabResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<LabResultDto>>> ListLabResults(Guid id)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            try
            {
                return Ok(await _patientCaseLabService.ListLabResultsAsync(id, clinicId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id:guid}/labresults")]
        [ProducesResponseType(typeof(LabResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LabResultDto>> UploadLabResult(Guid id, IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");
            var contentType = file.ContentType ?? "";
            if (!contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only PDF files are allowed for lab results.");

            var (_, clinicId) = await ResolveClinicIdAsync();
            try
            {
                var dto = await _patientCaseLabService.UploadLabResultAsync(
                    id,
                    clinicId,
                    User.GetUserId(),
                    file,
                    _env.ContentRootPath ?? "");
                return CreatedAtAction(nameof(ListLabResults), new { id }, dto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{id:guid}/labresults/{labId:guid}/file")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadLabResultFile(Guid id, Guid labId)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            try
            {
                var (bytes, contentTypeResolved, fileName) =
                    await _patientCaseLabService.DownloadLabResultFileAsync(
                        id,
                        labId,
                        clinicId,
                        _env.ContentRootPath ?? "",
                        User.GetUserId());
                return File(bytes, contentTypeResolved, fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        private Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync() =>
            _clinicContextService.ResolveClinicIdAsync(User);
    }
}
