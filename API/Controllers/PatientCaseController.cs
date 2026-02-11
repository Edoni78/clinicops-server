using ClinicOps.API.DTOs.MedicalReport;
using ClinicOps.API.DTOs.PatientCase;
using ClinicOps.API.DTOs.Vitals;
using ClinicOps.API.Hubs;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientCaseController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<ClinicHub> _hubContext;

        public PatientCaseController(ApplicationDbContext db, IHubContext<ClinicHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
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
                    CreatedAt = pc.CreatedAt
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
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _db.Clinics.Add(clinic);
                await _db.SaveChangesAsync();
            }
            return (true, defaultId);
        }
    }
}
