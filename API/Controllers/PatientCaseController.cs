using ClinicOps.API.DTOs.LabResult;
using ClinicOps.API.DTOs.MedicalReport;
using ClinicOps.API.DTOs.PatientCase;
using ClinicOps.API.DTOs.Vitals;
using ClinicOps.API.Hubs;
using ClinicOps.Application.Services.Common;
using ClinicOps.Application.Services.Gdpr;
using ClinicOps.Application.Services.Patient;
using ClinicOps.Application.Services.Pdf;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientCaseController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<ClinicHub> _hubContext;
        private readonly IWebHostEnvironment _env;
        private readonly IAuditLogService _auditLogService;
        private readonly IClinicContextService _clinicContextService;
        private readonly IPatientCaseReportService _patientCaseReportService;
        private readonly IPatientCaseWorkflowService _patientCaseWorkflowService;
        private readonly IPatientCaseQueryService _patientCaseQueryService;
        private readonly IPatientCaseCommandService _patientCaseCommandService;
        private readonly IPatientCaseLabService _patientCaseLabService;
        private readonly IPatientCasePdfFacadeService _patientCasePdfFacadeService;

        public PatientCaseController(
            ApplicationDbContext db,
            IHubContext<ClinicHub> hubContext,
            IWebHostEnvironment env,
            IAuditLogService auditLogService,
            IClinicContextService clinicContextService,
            IPatientCaseReportService patientCaseReportService,
            IPatientCaseWorkflowService patientCaseWorkflowService,
            IPatientCaseQueryService patientCaseQueryService,
            IPatientCaseCommandService patientCaseCommandService,
            IPatientCaseLabService patientCaseLabService,
            IPatientCasePdfFacadeService patientCasePdfFacadeService)
        {
            _db = db;
            _hubContext = hubContext;
            _env = env;
            _auditLogService = auditLogService;
            _clinicContextService = clinicContextService;
            _patientCaseReportService = patientCaseReportService;
            _patientCaseWorkflowService = patientCaseWorkflowService;
            _patientCaseQueryService = patientCaseQueryService;
            _patientCaseCommandService = patientCaseCommandService;
            _patientCaseLabService = patientCaseLabService;
            _patientCasePdfFacadeService = patientCasePdfFacadeService;
        }

        /// <summary>
        /// List patient cases for the clinic. Optional status filter (Waiting, InConsultation, Finished).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PatientCaseListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PatientCaseListItemDto>>> List([FromQuery] string? status = null)
        {
            var list = await _patientCaseQueryService.ListAsync(status, User);
            return Ok(list);
        }

        /// <summary>
        /// Get patient case by id with latest vitals and medical report (for nurse form / doctor panel).
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PatientCaseDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientCaseDetailDto>> GetById(Guid id)
        {
            try
            {
                var dto = await _patientCaseQueryService.GetByIdAsync(id, User);
                return Ok(dto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Nurse: submit/update vital signs for a patient case. Broadcasts to clinic via SignalR so doctor sees in real time.
        /// </summary>
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

            // Real-time: notify clinic (doctor panel) and optional case group
            await _hubContext.Clients
                .Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("VitalsUpdated", id, dto);
            await _hubContext.Clients
                .Group("case_" + id)
                .SendAsync("VitalsUpdated", id, dto);

            return Ok(dto);
        }

        /// <summary>
        /// Doctor: submit or update diagnosis and therapy for a patient case. Broadcasts via SignalR.
        /// </summary>
        [HttpPost("{id:guid}/report")]
        [Authorize(Roles = "Doctor,SuperAdmin")]
        [ProducesResponseType(typeof(MedicalReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedicalReportDto>> SubmitReport(Guid id, [FromBody] SubmitMedicalReportRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
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

            await _hubContext.Clients
                .Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("ReportUpdated", id, dto);
            await _hubContext.Clients
                .Group("case_" + id)
                .SendAsync("ReportUpdated", id, dto);

            return Ok(dto);
        }

        /// <summary>
        /// Doctor: read EMR report for a case.
        /// </summary>
        [HttpGet("{id:guid}/report")]
        [Authorize(Roles = "Doctor,SuperAdmin")]
        [ProducesResponseType(typeof(MedicalReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedicalReportDto>> GetReport(Guid id)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            try
            {
                var dto = await _patientCaseReportService.GetReportAsync(id, clinicId, userId);
                return Ok(dto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Doctor: delete EMR report for a case.
        /// </summary>
        [HttpDelete("{id:guid}/report")]
        [Authorize(Roles = "Doctor,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            try
            {
                await _patientCaseReportService.DeleteReportAsync(id, clinicId, userId);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            await _hubContext.Clients
                .Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("ReportDeleted", id);
            await _hubContext.Clients
                .Group("case_" + id)
                .SendAsync("ReportDeleted", id);

            return NoContent();
        }

        /// <summary>
        /// Clinic staff/SuperAdmin: delete a patient case and all cascade children (vitals/report/labs/payment).
        /// </summary>
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

            await _hubContext.Clients
                .Group(ClinicHub.GroupPrefix + resolvedClinicId)
                .SendAsync("CaseDeleted", id);
            await _hubContext.Clients
                .Group("case_" + id)
                .SendAsync("CaseDeleted", id);

            return NoContent();
        }

        /// <summary>
        /// Update patient case status (Waiting → InConsultation → Finished).
        /// </summary>
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
                statusEnum = await _patientCaseWorkflowService.UpdateStatusAsync(id, statusEnum, clinicId);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            await _hubContext.Clients
                .Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("CaseStatusChanged", id, statusEnum.ToString());
            await _hubContext.Clients
                .Group("case_" + id)
                .SendAsync("CaseStatusChanged", id, statusEnum.ToString());

            return Ok(new { id, status = statusEnum.ToString() });
        }

        /// <summary>
        /// Attach an existing clinic service to this case (doctor selects service; nurse sees name/price on case list).
        /// Accepts <c>serviceId</c> as query param and/or JSON body <c>{ "serviceId": "guid" }</c>.
        /// </summary>
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
            Guid attachedServiceId;
            string attachedServiceName;
            decimal attachedServicePrice;
            try
            {
                (attachedServiceId, attachedServiceName, attachedServicePrice) =
                    await _patientCaseCommandService.AttachServiceAsync(id, clinicId, resolved.Value);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(new
            {
                id,
                serviceId = attachedServiceId,
                serviceName = attachedServiceName,
                servicePrice = attachedServicePrice
            });
        }

        /// <summary>
        /// Generate and download PDF report for the patient case (HTML to PDF via PuppeteerSharp).
        /// API: GET /api/PatientCase/{id}/pdf  (Authorization: Bearer token)
        /// </summary>
        [HttpGet("{id:guid}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadCaseReportPdf(Guid id)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fallbackDoctorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")?.Value;
            try
            {
                var (fileBytes, fileName) = await _patientCasePdfFacadeService
                    .GenerateCaseReportPdfAsync(id, clinicId, baseUrl, fallbackDoctorUserId);
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// List lab result PDFs for a patient case. Any authenticated user with access to the case can list.
        /// </summary>
        [HttpGet("{id:guid}/labresults")]
        [ProducesResponseType(typeof(List<LabResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<LabResultDto>>> ListLabResults(Guid id)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            List<LabResultDto> list;
            try
            {
                list = await _patientCaseLabService.ListLabResultsAsync(id, clinicId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            return Ok(list);
        }

        /// <summary>
        /// Upload a lab result PDF for a patient case. Any authenticated user with access to the case can upload.
        /// </summary>
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            LabResultDto dto;
            try
            {
                dto = await _patientCaseLabService.UploadLabResultAsync(
                    id,
                    clinicId,
                    userId,
                    file,
                    _env.ContentRootPath ?? "");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return CreatedAtAction(nameof(ListLabResults), new { id }, dto);
        }

        /// <summary>
        /// Download a single lab result PDF file. Authorized if user has access to the patient case.
        /// </summary>
        [HttpGet("{id:guid}/labresults/{labId:guid}/file")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadLabResultFile(Guid id, Guid labId)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            try
            {
                var (bytes, contentTypeResolved, fileName) =
                    await _patientCaseLabService.DownloadLabResultFileAsync(
                        id,
                        labId,
                        clinicId,
                        _env.ContentRootPath ?? "",
                        userId);
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

        private async Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync()
        {
            return await _clinicContextService.ResolveClinicIdAsync(User);
        }

    }
}
