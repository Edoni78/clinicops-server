using ClinicOps.API.DTOs.Patient;
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
        private readonly ApplicationDbContext _db;

        public PatientController(IPatientService patientService, ApplicationDbContext db)
        {
            _patientService = patientService;
            _db = db;
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
            // Get clinic ID from JWT claims or request body
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            Guid clinicId;

            // Check if user is SuperAdmin (no clinicId in token)
            if (string.IsNullOrEmpty(clinicIdClaim))
            {
                // SuperAdmin: use clinicId from request body, or use default test clinic GUID
                if (request.ClinicId.HasValue)
                {
                    clinicId = request.ClinicId.Value;
                }
                else
                {
                    // Use default test clinic GUID for SuperAdmin: 11111111-1111-1111-1111-111111111111
                    clinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                    
                    // Verify clinic exists, if not create it
                    var defaultClinic = await _db.Clinics.FindAsync(clinicId);
                    if (defaultClinic == null)
                    {
                        defaultClinic = new Clinic
                        {
                            Id = clinicId,
                            Name = "Default Test Clinic",
                            Address = "123 Test Street",
                            Phone = "+1234567890",
                            ClinicMode = ClinicMode.FullTeam,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _db.Clinics.Add(defaultClinic);
                        await _db.SaveChangesAsync();
                    }
                }
            }
            else
            {
                // Clinic user: use clinicId from token
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
            // Get clinic ID from JWT claims
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
            Guid? filterClinicId = null;

            // Check if user is SuperAdmin (no clinicId in token)
            if (string.IsNullOrEmpty(clinicIdClaim))
            {
                // SuperAdmin: can filter by clinicId query parameter, or see all patients
                filterClinicId = clinicId;
            }
            else
            {
                // Clinic user: only see their clinic's patients
                if (!Guid.TryParse(clinicIdClaim, out var userClinicId))
                {
                    return BadRequest("Invalid clinic ID in token.");
                }
                filterClinicId = userClinicId;
            }

            // Query patients
            var query = _db.Patients
                .Include(p => p.Clinic)
                .Where(p => p.IsActive);

            // Apply clinic filter if specified
            if (filterClinicId.HasValue)
            {
                query = query.Where(p => p.ClinicId == filterClinicId.Value);
            }

            var patients = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Get latest patient case for each patient
            var patientIds = patients.Select(p => p.Id).ToList();
            var latestCases = await _db.PatientCases
                .Where(pc => patientIds.Contains(pc.PatientId))
                .GroupBy(pc => pc.PatientId)
                .Select(g => g.OrderByDescending(pc => pc.CreatedAt).First())
                .ToListAsync();

            // Map to DTOs
            var result = patients.Select(p =>
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

            return Ok(result);
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
            var (_, clinicId) = await ResolveClinicIdAsync();
            var isDoctor = User.IsInRole("Doctor");

            if (doctorView && !isDoctor)
                return Forbid();

            var effectiveDoctorView = doctorView && isDoctor;

            var patient = await _db.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.ClinicId == clinicId && p.IsActive);

            if (patient == null)
                return NotFound("Patient not found.");

            var cases = await _db.PatientCases
                .AsNoTracking()
                .Where(pc => pc.PatientId == id && pc.ClinicId == clinicId)
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

            return Ok(new PatientEmrDto
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
            });
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
            var patientQuery = _db.Patients.Where(p => p.Id == id && p.IsActive);

            if (User.IsInRole("SuperAdmin"))
            {
                if (clinicId.HasValue)
                    patientQuery = patientQuery.Where(p => p.ClinicId == clinicId.Value);
            }
            else
            {
                // Non-superadmin users can only delete patients from their own clinic.
                var clinicIdClaim = User.FindFirst("clinicId")?.Value;
                if (string.IsNullOrWhiteSpace(clinicIdClaim) || !Guid.TryParse(clinicIdClaim, out var userClinicId))
                    return Forbid();

                patientQuery = patientQuery.Where(p => p.ClinicId == userClinicId);
            }

            var patient = await patientQuery.FirstOrDefaultAsync();
            if (patient == null)
                return NotFound("Patient not found.");

            patient.IsActive = false;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private async Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync()
        {
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;
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
