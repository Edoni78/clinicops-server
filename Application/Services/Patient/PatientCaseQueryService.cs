using ClinicOps.API.DTOs.PatientCase;
using ClinicOps.Application.Services.ClinicSettings;
using ClinicOps.Application.Services.Gdpr;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientCaseQueryService : IPatientCaseQueryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public PatientCaseQueryService(ApplicationDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task<List<PatientCaseListItemDto>> ListAsync(string? status, ClaimsPrincipal user)
        {
            var (_, clinicId) = await ResolveClinicIdAsync(user);
            var query = _db.PatientCases
                .AsNoTracking()
                .Where(pc => pc.ClinicId == clinicId);

            if (user.IsInRole("Doctor") && !user.IsInRole("SuperAdmin"))
            {
                var doctorUserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value;
                if (!string.IsNullOrWhiteSpace(doctorUserId))
                    query = query.Where(pc => pc.AssignedDoctorUserId == doctorUserId);
            }

            if (!string.IsNullOrEmpty(status) && PatientCaseStatusParser.TryParse(status, out var statusEnum))
                query = query.Where(pc => pc.Status == statusEnum);

            return await query
                .OrderByDescending(pc => pc.CreatedAt)
                .Select(pc => new PatientCaseListItemDto
                {
                    Id = pc.Id,
                    PatientId = pc.PatientId,
                    PatientFirstName = pc.Patient.FirstName,
                    PatientLastName = pc.Patient.LastName,
                    Status = pc.Status.ToString(),
                    CreatedAt = pc.CreatedAt,
                    CompletedAt = pc.CompletedAt,
                    ServiceId = pc.ServiceId,
                    ServiceName = pc.ServiceId != null ? pc.Service!.Name : null,
                    ServicePrice = pc.ServiceId != null ? pc.Service!.Price : null,
                    AssignedDoctorUserId = pc.AssignedDoctorUserId,
                    AssignedDoctorName = pc.AssignedDoctor != null
                        ? (pc.AssignedDoctor.DoctorDisplayName ?? pc.AssignedDoctor.Email ?? pc.AssignedDoctor.UserName)
                        : null
                })
                .ToListAsync();
        }

        public async Task<PatientCaseDetailDto> GetByIdAsync(Guid caseId, ClaimsPrincipal user)
        {
            var (_, clinicId) = await ResolveClinicIdAsync(user);
            var @case = await _db.PatientCases
                .Include(pc => pc.Patient)
                .Include(pc => pc.Clinic)
                .Include(pc => pc.Service)
                .Include(pc => pc.AssignedDoctor)
                .FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);

            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            if (user.IsInRole("Doctor") && !user.IsInRole("SuperAdmin"))
            {
                var doctorUserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value;
                if (!string.IsNullOrWhiteSpace(doctorUserId) && @case.AssignedDoctorUserId != doctorUserId)
                    throw new UnauthorizedAccessException("This patient case is assigned to another doctor.");
            }

            var latestVitals = await _db.VitalSigns
                .Where(v => v.PatientCaseId == caseId)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            var report = await _db.MedicalReports
                .FirstOrDefaultAsync(m => m.PatientCaseId == caseId);

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value;
            await _auditLogService.TryLogAsync("MedicalRecordViewed", "PatientCase", caseId.ToString(), clinicId, userId);

            return new PatientCaseDetailDto
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
                AssignedDoctorUserId = @case.AssignedDoctorUserId,
                AssignedDoctorName = @case.AssignedDoctor != null
                    ? (@case.AssignedDoctor.DoctorDisplayName ?? @case.AssignedDoctor.Email ?? @case.AssignedDoctor.UserName)
                    : null,
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
                },
                VitalPreferences = ClinicVitalPreferencesMapper.ToDto(@case.Clinic)
            };
        }

        private async Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync(ClaimsPrincipal user)
        {
            var clinicIdClaim = user.FindFirst("clinicId")?.Value;
            if (!string.IsNullOrEmpty(clinicIdClaim) && Guid.TryParse(clinicIdClaim, out var fromToken))
                return (false, fromToken);

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
    }
}
