using ClinicOps.API.DTOs.LabResult;
using ClinicOps.API.DTOs.MedicalReport;
using ClinicOps.API.DTOs.PatientCase;
using ClinicOps.API.DTOs.Vitals;
using ClinicOps.API.Hubs;
using ClinicOps.Application.Services.Pdf;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
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
        private readonly ICaseReportPdfService _pdfService;
        private readonly IWebHostEnvironment _env;

        public PatientCaseController(ApplicationDbContext db, IHubContext<ClinicHub> hubContext, ICaseReportPdfService pdfService, IWebHostEnvironment env)
        {
            _db = db;
            _hubContext = hubContext;
            _pdfService = pdfService;
            _env = env;
        }

        /// <summary>
        /// List patient cases for the clinic. Optional status filter (Waiting, InProgress, InConsultation, Completed, Finished).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PatientCaseListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PatientCaseListItemDto>>> List([FromQuery] string? status = null)
        {
            var (_, clinicId) = await ResolveClinicIdAsync();
            var query = _db.PatientCases
                .Include(pc => pc.Patient)
                .Where(pc => pc.ClinicId == clinicId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PatientCaseStatus>(status, ignoreCase: true, out var statusEnum))
                query = query.Where(pc => pc.Status == statusEnum);

            var list = await query
                .OrderByDescending(pc => pc.CreatedAt)
                .Select(pc => new PatientCaseListItemDto
                {
                    Id = pc.Id,
                    PatientId = pc.PatientId,
                    PatientFirstName = pc.Patient.FirstName,
                    PatientLastName = pc.Patient.LastName,
                    Status = pc.Status.ToString(),
                    CreatedAt = pc.CreatedAt,
                    ServiceId = pc.ServiceId,
                    ServiceName = pc.Service != null ? pc.Service.Name : null,
                    ServicePrice = pc.Service != null ? pc.Service.Price : null
                })
                .ToListAsync();

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
            var (_, clinicId) = await ResolveClinicIdAsync();
            var @case = await _db.PatientCases
                .Include(pc => pc.Patient)
                .Include(pc => pc.Clinic)
                .Include(pc => pc.Service)
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.ClinicId == clinicId);

            if (@case == null)
                return NotFound("Patient case not found.");

            var latestVitals = await _db.VitalSigns
                .Where(v => v.PatientCaseId == id)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            var report = await _db.MedicalReports
                .FirstOrDefaultAsync(m => m.PatientCaseId == id);

            return Ok(new PatientCaseDetailDto
            {
                Id = @case.Id,
                ClinicId = @case.ClinicId,
                PatientId = @case.PatientId,
                PatientFirstName = @case.Patient.FirstName,
                PatientLastName = @case.Patient.LastName,
                PatientDateOfBirth = @case.Patient.DateOfBirth,
                PatientPhone = @case.Patient.Phone,
                PatientGender = @case.Patient.Gender,
                Status = @case.Status.ToString(),
                CreatedAt = @case.CreatedAt,
                CompletedAt = @case.CompletedAt,
                Notes = @case.Notes,
                ServiceId = @case.ServiceId,
                ServiceName = @case.Service?.Name,
                ServicePrice = @case.Service?.Price,
                LatestVitals = latestVitals == null ? null : new VitalSignsSummaryDto
                {
                    Id = latestVitals.Id,
                    WeightKg = latestVitals.WeightKg,
                    SystolicPressure = latestVitals.SystolicPressure,
                    DiastolicPressure = latestVitals.DiastolicPressure,
                    TemperatureC = latestVitals.TemperatureC,
                    HeartRate = latestVitals.HeartRate,
                    RecordedAt = latestVitals.RecordedAt
                },
                MedicalReport = report == null ? null : new MedicalReportSummaryDto
                {
                    Id = report.Id,
                    Anamneza = report.Anamneza,
                    Diagnosis = report.Diagnosis,
                    Therapy = report.Therapy,
                    CreatedAt = report.CreatedAt,
                    DoctorId = report.DoctorUserId ?? ""
                }
            });
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
            if (await IsSoloDoctorClinicAsync(clinicId))
                return BadRequest("This clinic mode does not include nurse workflow.");
            var @case = await _db.PatientCases
                .Include(pc => pc.Patient)
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.ClinicId == clinicId);

            if (@case == null)
                return NotFound("Patient case not found.");

            var vitals = new VitalSigns
            {
                ClinicId = clinicId,
                PatientCaseId = id,
                WeightKg = request.WeightKg,
                SystolicPressure = request.SystolicPressure,
                DiastolicPressure = request.DiastolicPressure,
                TemperatureC = request.TemperatureC,
                HeartRate = request.HeartRate,
                RecordedAt = DateTime.UtcNow
            };
            _db.VitalSigns.Add(vitals);
            await _db.SaveChangesAsync();

            var dto = new VitalSignsDto
            {
                Id = vitals.Id,
                PatientCaseId = id,
                WeightKg = vitals.WeightKg,
                SystolicPressure = vitals.SystolicPressure,
                DiastolicPressure = vitals.DiastolicPressure,
                TemperatureC = vitals.TemperatureC,
                HeartRate = vitals.HeartRate,
                RecordedAt = vitals.RecordedAt
            };

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
            var @case = await _db.PatientCases
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.ClinicId == clinicId);

            if (@case == null)
                return NotFound("Patient case not found.");

            var existing = await _db.MedicalReports.FirstOrDefaultAsync(m => m.PatientCaseId == id);
            MedicalReport report;
            if (existing != null)
            {
                existing.Anamneza = request.Anamneza;
                existing.Diagnosis = request.Diagnosis;
                existing.Therapy = request.Therapy;
                existing.DoctorUserId = userId;
                report = existing;
            }
            else
            {
                report = new MedicalReport
                {
                    ClinicId = clinicId,
                    PatientCaseId = id,
                    Anamneza = request.Anamneza,
                    Diagnosis = request.Diagnosis,
                    Therapy = request.Therapy,
                    DoctorId = Guid.Empty,
                    DoctorUserId = userId
                };
                _db.MedicalReports.Add(report);
            }
            await _db.SaveChangesAsync();

            var dto = new MedicalReportDto
            {
                Id = report.Id,
                PatientCaseId = id,
                Anamneza = report.Anamneza,
                Diagnosis = report.Diagnosis,
                Therapy = report.Therapy,
                CreatedAt = report.CreatedAt,
                DoctorId = report.DoctorUserId ?? userId
            };

            await _hubContext.Clients
                .Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("ReportUpdated", id, dto);
            await _hubContext.Clients
                .Group("case_" + id)
                .SendAsync("ReportUpdated", id, dto);

            return Ok(dto);
        }

        /// <summary>
        /// Update patient case status (e.g. InConsultation when doctor starts, Completed when done).
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
        {
            if (!Enum.TryParse<PatientCaseStatus>(status, ignoreCase: true, out var statusEnum))
                return BadRequest("Invalid status. Use: Waiting, InProgress, InConsultation, Completed, Finished.");
            var (_, clinicId) = await ResolveClinicIdAsync();
            var @case = await _db.PatientCases
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.ClinicId == clinicId);

            if (@case == null)
                return NotFound("Patient case not found.");

            @case.Status = statusEnum;
            if (statusEnum == PatientCaseStatus.Completed || statusEnum == PatientCaseStatus.Finished)
                @case.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

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
            var @case = await _db.PatientCases.FirstOrDefaultAsync(pc => pc.Id == id && pc.ClinicId == clinicId);
            if (@case == null)
                return NotFound("Patient case not found.");

            var service = await _db.Services.FirstOrDefaultAsync(s =>
                s.Id == resolved.Value && s.ClinicId == clinicId && s.IsActive);
            if (service == null)
                return BadRequest("Service not found, inactive, or not in this clinic.");

            @case.ServiceId = service.Id;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id,
                serviceId = service.Id,
                serviceName = service.Name,
                servicePrice = service.Price
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
            var isSoloDoctor = await IsSoloDoctorClinicAsync(clinicId);
            var @case = await _db.PatientCases
                .Include(pc => pc.Patient)
                .Include(pc => pc.Clinic)
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.ClinicId == clinicId);

            if (@case == null)
                return NotFound("Patient case not found.");

            var latestVitals = await _db.VitalSigns
                .Where(v => v.PatientCaseId == id)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();
            var report = await _db.MedicalReports.FirstOrDefaultAsync(m => m.PatientCaseId == id);

            string? doctorDisplayName = null;
            string? signatureUrl = null;
            string? stampUrl = null;
            string? signatureDataUri = null;
            string? stampDataUri = null;
            if (report != null && !string.IsNullOrEmpty(report.DoctorUserId))
            {
                var doctorUser = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == report.DoctorUserId);
                if (doctorUser != null)
                {
                    doctorDisplayName = doctorUser.DoctorDisplayName ?? doctorUser.Email;
                    signatureUrl = doctorUser.SignatureUrl;
                    stampUrl = doctorUser.StampUrl;
                    signatureDataUri = TryReadFileAsDataUri(_env, signatureUrl);
                    stampDataUri = TryReadFileAsDataUri(_env, stampUrl);
                }
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var model = new PatientCaseReportModel
            {
                ClinicName = @case.Clinic?.Name ?? "",
                ClinicAddress = @case.Clinic?.Address,
                ClinicPhone = @case.Clinic?.Phone,
                ClinicLogoUrl = @case.Clinic?.LogoUrl,
                PatientFirstName = @case.Patient.FirstName,
                PatientLastName = @case.Patient.LastName,
                PatientDateOfBirth = @case.Patient.DateOfBirth,
                PatientGender = @case.Patient.Gender,
                PatientPhone = @case.Patient.Phone,
                Status = @case.Status.ToString(),
                CreatedAt = @case.CreatedAt,
                Notes = @case.Notes,
                LatestVitals = latestVitals == null ? null : new VitalsModel
                {
                    WeightKg = latestVitals.WeightKg,
                    SystolicPressure = latestVitals.SystolicPressure,
                    DiastolicPressure = latestVitals.DiastolicPressure,
                    TemperatureC = latestVitals.TemperatureC,
                    HeartRate = latestVitals.HeartRate,
                    RecordedAt = latestVitals.RecordedAt
                },
                MedicalReport = report == null ? null : new MedicalReportModel
                {
                    Anamneza = report.Anamneza,
                    Diagnosis = report.Diagnosis,
                    Therapy = report.Therapy,
                    CreatedAt = report.CreatedAt
                },
                BaseUrl = baseUrl,
                DoctorDisplayName = doctorDisplayName,
                SignatureUrl = signatureUrl,
                StampUrl = stampUrl,
                SignatureDataUri = signatureDataUri,
                StampDataUri = stampDataUri
            };

            var pdfBytes = await _pdfService.GenerateCaseReportPdfAsync(model);

            // If the case has lab result PDFs, append them as additional pages (first page = report, then labs)
            var labResults = await _db.LabResults
                .Where(l => l.PatientCaseId == id)
                .OrderBy(l => l.UploadedAt)
                .ToListAsync();
            if (!isSoloDoctor && labResults.Count > 0)
            {
                pdfBytes = MergeReportWithLabPdfs(pdfBytes, labResults);
            }

            var fileName = $"CaseReport_{@case.Patient.LastName}_{@case.Patient.FirstName}_{id:N}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
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
            if (await IsSoloDoctorClinicAsync(clinicId))
                return BadRequest("This clinic mode does not include laboratory workflow.");
            var @case = await _db.PatientCases.FirstOrDefaultAsync(pc => pc.Id == id && pc.ClinicId == clinicId);
            if (@case == null)
                return NotFound("Patient case not found.");

            var list = await _db.LabResults
                .Where(l => l.PatientCaseId == id)
                .OrderBy(l => l.UploadedAt)
                .Select(l => new LabResultDto
                {
                    Id = l.Id,
                    PatientCaseId = l.PatientCaseId,
                    FileName = l.FileName,
                    DownloadUrl = $"/api/PatientCase/{id}/labresults/{l.Id}/file",
                    ContentType = l.ContentType,
                    UploadedAt = l.UploadedAt,
                    UploadedById = l.UploadedById
                })
                .ToListAsync();
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
            if (await IsSoloDoctorClinicAsync(clinicId))
                return BadRequest("This clinic mode does not include laboratory workflow.");
            var @case = await _db.PatientCases.FirstOrDefaultAsync(pc => pc.Id == id && pc.ClinicId == clinicId);
            if (@case == null)
                return NotFound("Patient case not found.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var labId = Guid.NewGuid();
            var relativePath = Path.Combine("LabUploads", id.ToString("N"), labId.ToString("N") + ".pdf").Replace('\\', '/');

            var labResult = new LabResult
            {
                Id = labId,
                ClinicId = clinicId,
                PatientCaseId = id,
                FileName = Path.GetFileName(file.FileName) ?? $"lab_{labId:N}.pdf",
                FilePath = relativePath,
                ContentType = "application/pdf",
                UploadedAt = DateTime.UtcNow,
                UploadedById = userId
            };
            _db.LabResults.Add(labResult);

            var fullPath = Path.Combine(_env.ContentRootPath ?? "", relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(ListLabResults), new { id }, new LabResultDto
            {
                Id = labResult.Id,
                PatientCaseId = labResult.PatientCaseId,
                FileName = labResult.FileName,
                DownloadUrl = $"/api/PatientCase/{id}/labresults/{labResult.Id}/file",
                ContentType = labResult.ContentType,
                UploadedAt = labResult.UploadedAt,
                UploadedById = labResult.UploadedById
            });
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
            if (await IsSoloDoctorClinicAsync(clinicId))
                return BadRequest("This clinic mode does not include laboratory workflow.");
            var lab = await _db.LabResults.FirstOrDefaultAsync(l =>
                l.Id == labId && l.PatientCaseId == id && l.ClinicId == clinicId);
            if (lab == null)
                return NotFound("Lab result not found.");

            var fullPath = Path.Combine(_env.ContentRootPath ?? "", lab.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                return NotFound("Lab result file not found on disk.");

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, lab.ContentType ?? "application/pdf", lab.FileName);
        }

        private byte[] MergeReportWithLabPdfs(byte[] reportPdfBytes, List<LabResult> labResults)
        {
            using var outputDoc = new PdfDocument();
            using (var reportStream = new MemoryStream(reportPdfBytes))
            using (var reportDoc = PdfReader.Open(reportStream, PdfDocumentOpenMode.Import))
            {
                for (int i = 0; i < reportDoc.PageCount; i++)
                    outputDoc.AddPage(reportDoc.Pages[i]);
            }
            var contentRoot = _env.ContentRootPath ?? "";
            foreach (var lab in labResults)
            {
                var fullPath = Path.Combine(contentRoot, lab.FilePath.Replace('/', Path.DirectorySeparatorChar));
                if (!System.IO.File.Exists(fullPath)) continue;
                using (var labStream = System.IO.File.OpenRead(fullPath))
                using (var labDoc = PdfReader.Open(labStream, PdfDocumentOpenMode.Import))
                {
                    for (int i = 0; i < labDoc.PageCount; i++)
                        outputDoc.AddPage(labDoc.Pages[i]);
                }
            }
            using var ms = new MemoryStream();
            outputDoc.Save(ms, false);
            return ms.ToArray();
        }

        private async Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync()
        {
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            if (!string.IsNullOrEmpty(clinicIdClaim) && Guid.TryParse(clinicIdClaim, out var fromToken))
                return (false, fromToken);

            // SuperAdmin: default clinic
            var defaultId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var clinic = await _db.Clinics.FindAsync(defaultId);
            if (clinic == null)
            {
                clinic = new Clinic
                {
                    Id = defaultId,
                    Name = "Default Test Clinic",
                    Address = "123 Test Street",
                    Phone = "+1234567890",
                    ClinicMode = ClinicMode.FullTeam,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _db.Clinics.Add(clinic);
                await _db.SaveChangesAsync();
            }
            return (true, defaultId);
        }

        private async Task<bool> IsSoloDoctorClinicAsync(Guid clinicId)
        {
            var mode = await _db.Clinics
                .Where(c => c.Id == clinicId)
                .Select(c => c.ClinicMode)
                .FirstOrDefaultAsync();
            return mode == ClinicMode.SoloDoctor;
        }

        private static string? TryReadFileAsDataUri(IWebHostEnvironment env, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;
            var root = env.WebRootPath ?? env.ContentRootPath;
            if (string.IsNullOrEmpty(root)) return null;
            var path = System.IO.Path.Combine(root, relativePath.TrimStart('/', '\\'));
            if (!System.IO.File.Exists(path)) return null;
            try
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                var base64 = Convert.ToBase64String(bytes);
                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                var mime = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "image/png"
                };
                return $"data:{mime};base64,{base64}";
            }
            catch
            {
                return null;
            }
        }
    }
}
