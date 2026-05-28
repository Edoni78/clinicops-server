using ClinicOps.API.DTOs.Patient;
using ClinicOps.Application.Services.Gdpr;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientQueryService : IPatientQueryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public PatientQueryService(ApplicationDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task<List<PatientResponseDto>> GetAllPatientsAsync(Guid? clinicId, ClaimsPrincipal user)
        {
            var clinicIdClaim = user.FindFirst("clinicId")?.Value;
            Guid? filterClinicId = null;

            if (string.IsNullOrEmpty(clinicIdClaim))
            {
                filterClinicId = clinicId;
            }
            else
            {
                if (!Guid.TryParse(clinicIdClaim, out var userClinicId))
                    throw new InvalidOperationException("Invalid clinic ID in token.");

                filterClinicId = userClinicId;
            }

            var query = _db.Patients
                .Include(p => p.Clinic)
                .Where(p => p.IsActive);

            if (filterClinicId.HasValue)
                query = query.Where(p => p.ClinicId == filterClinicId.Value);

            var patients = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var patientIds = patients.Select(p => p.Id).ToList();
            var latestCases = await _db.PatientCases
                .Where(pc => patientIds.Contains(pc.PatientId))
                .GroupBy(pc => pc.PatientId)
                .Select(g => g.OrderByDescending(pc => pc.CreatedAt).First())
                .ToListAsync();

            return patients.Select(p =>
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
        }

        public async Task<PatientEmrDto> GetPatientEmrAsync(Guid patientId, bool doctorView, ClaimsPrincipal user)
        {
            var (_, clinicId) = await ResolveClinicIdAsync(user);
            var isDoctor = user.IsInRole("Doctor");

            if (doctorView && !isDoctor)
                throw new UnauthorizedAccessException("Forbidden");

            var effectiveDoctorView = doctorView && isDoctor;

            var patient = await _db.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId && p.IsActive);

            if (patient == null)
                throw new KeyNotFoundException("Patient not found.");

            var cases = await _db.PatientCases
                .AsNoTracking()
                .Where(pc => pc.PatientId == patientId && pc.ClinicId == clinicId)
                .OrderByDescending(pc => pc.CreatedAt)
                .ToListAsync();

            var caseIds = cases.Select(c => c.Id).ToList();

            var vitalsByCase = await _db.VitalSigns
                .AsNoTracking()
                .Where(v => caseIds.Contains(v.PatientCaseId))
                .OrderBy(v => v.RecordedAt)
                .GroupBy(v => v.PatientCaseId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(v => new PatientEmrVitalsDto
                    {
                        Id = v.Id,
                        WeightKg = v.WeightKg,
                        SystolicPressure = v.SystolicPressure,
                        DiastolicPressure = v.DiastolicPressure,
                        TemperatureC = v.TemperatureC,
                        HeartRate = v.HeartRate,
                        RecordedAt = v.RecordedAt
                    }).ToList());

            var reports = await _db.MedicalReports
                .AsNoTracking()
                .Where(r => caseIds.Contains(r.PatientCaseId))
                .ToListAsync();

            var doctorUserIds = reports
                .Select(r => r.DoctorUserId)
                .Where(idValue => !string.IsNullOrWhiteSpace(idValue))
                .Distinct()
                .Cast<string>()
                .ToList();

            var doctorLookup = await _db.Users
                .AsNoTracking()
                .Where(u => doctorUserIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.DoctorDisplayName ?? u.Email ?? u.UserName })
                .ToDictionaryAsync(u => u.Id, u => u.Name ?? u.Id);

            var reportLookup = reports.ToDictionary(r => r.PatientCaseId, r => r);

            var history = cases.Select(pc =>
            {
                reportLookup.TryGetValue(pc.Id, out var report);
                vitalsByCase.TryGetValue(pc.Id, out var vitals);

                var doctorUserId = report?.DoctorUserId;
                var doctorName = !string.IsNullOrWhiteSpace(doctorUserId) && doctorLookup.TryGetValue(doctorUserId, out var resolvedName)
                    ? resolvedName
                    : null;

                return new PatientEmrConsultDto
                {
                    PatientCaseId = pc.Id,
                    ConsultDate = pc.CompletedAt ?? report?.CreatedAt ?? pc.CreatedAt,
                    CaseStatus = pc.Status.ToString(),
                    CanEdit = effectiveDoctorView,
                    Notes = effectiveDoctorView ? pc.Notes : null,
                    DoctorUserId = effectiveDoctorView ? doctorUserId : null,
                    DoctorName = doctorName,
                    Anamneza = effectiveDoctorView ? report?.Anamneza : null,
                    Diagnosis = report?.Diagnosis,
                    Therapy = report?.Therapy,
                    ReportCreatedAt = report?.CreatedAt,
                    Vitals = vitals ?? new List<PatientEmrVitalsDto>()
                };
            }).ToList();

            var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            await _auditLogService.TryLogAsync("MedicalRecordViewed", "PatientEMR", patientId.ToString(), clinicId, currentUserId);

            return new PatientEmrDto
            {
                PatientId = patient.Id,
                ClinicId = patient.ClinicId,
                IsDoctorView = effectiveDoctorView,
                IsReadOnly = !effectiveDoctorView,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                Phone = patient.Phone,
                History = history
            };
        }

        public async Task DeletePatientAsync(Guid patientId, Guid? clinicId, ClaimsPrincipal user)
        {
            var patientQuery = _db.Patients.Where(p => p.Id == patientId && p.IsActive);

            if (user.IsInRole("SuperAdmin"))
            {
                if (clinicId.HasValue)
                    patientQuery = patientQuery.Where(p => p.ClinicId == clinicId.Value);
            }
            else
            {
                var clinicIdClaim = user.FindFirst("clinicId")?.Value;
                if (string.IsNullOrWhiteSpace(clinicIdClaim) || !Guid.TryParse(clinicIdClaim, out var userClinicId))
                    throw new UnauthorizedAccessException("Forbidden");

                patientQuery = patientQuery.Where(p => p.ClinicId == userClinicId);
            }

            var patient = await patientQuery.FirstOrDefaultAsync();
            if (patient == null)
                throw new KeyNotFoundException("Patient not found.");

            patient.IsActive = false;
            await _db.SaveChangesAsync();

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            await _auditLogService.TryLogAsync("PatientDeleted", "Patient", patient.Id.ToString(), patient.ClinicId, userId);
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
                    ClinicMode = Domain.Enums.ClinicMode.FullTeam,
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
