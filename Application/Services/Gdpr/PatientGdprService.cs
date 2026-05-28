using ClinicOps.API.DTOs.Gdpr;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Gdpr
{
    public class PatientGdprService : IPatientGdprService
    {
        private readonly ApplicationDbContext _db;

        public PatientGdprService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PatientGdprExportDto> ExportPatientDataAsync(Guid patientId, Guid currentClinicId, string? currentUserId)
        {
            var patient = await EnsureClinicPatientAsync(patientId, currentClinicId);

            var cases = await _db.PatientCases
                .AsNoTracking()
                .Where(c => c.PatientId == patientId && c.ClinicId == currentClinicId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var caseIds = cases.Select(c => c.Id).ToList();

            var emrRecords = await _db.MedicalReports
                .AsNoTracking()
                .Where(m => caseIds.Contains(m.PatientCaseId) && m.ClinicId == currentClinicId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var labResults = await _db.LabResults
                .AsNoTracking()
                .Where(l => caseIds.Contains(l.PatientCaseId) && l.ClinicId == currentClinicId)
                .OrderByDescending(l => l.UploadedAt)
                .ToListAsync();

            var consents = await _db.PatientConsents
                .AsNoTracking()
                .Where(c => c.PatientId == patientId && c.ClinicId == currentClinicId)
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new PatientConsentDto
                {
                    Id = c.Id,
                    PatientId = c.PatientId,
                    ClinicId = c.ClinicId,
                    HasGivenConsent = c.HasGivenConsent,
                    ConsentType = c.ConsentType,
                    CreatedAtUtc = c.CreatedAtUtc,
                    WithdrawnAtUtc = c.WithdrawnAtUtc,
                    GivenByUserId = c.GivenByUserId,
                    Notes = c.Notes
                })
                .ToListAsync();

            return new PatientGdprExportDto
            {
                Patient = new PatientExportProfileDto
                {
                    Id = patient.Id,
                    ClinicId = patient.ClinicId,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    Phone = patient.Phone,
                    CreatedAt = patient.CreatedAt,
                    IsActive = patient.IsActive
                },
                Cases = cases.Select(c => new PatientExportCaseDto
                {
                    Id = c.Id,
                    ClinicId = c.ClinicId,
                    PatientId = c.PatientId,
                    Status = c.Status.ToString(),
                    CreatedAt = c.CreatedAt,
                    CompletedAt = c.CompletedAt,
                    Notes = c.Notes,
                    ServiceId = c.ServiceId
                }).ToList(),
                EmrRecords = emrRecords.Select(m => new PatientExportMedicalReportDto
                {
                    Id = m.Id,
                    PatientCaseId = m.PatientCaseId,
                    ClinicId = m.ClinicId,
                    Anamneza = m.Anamneza,
                    Diagnosis = m.Diagnosis,
                    Therapy = m.Therapy,
                    CreatedAt = m.CreatedAt,
                    DoctorUserId = m.DoctorUserId
                }).ToList(),
                LabResults = labResults.Select(l => new PatientExportLabResultDto
                {
                    Id = l.Id,
                    PatientCaseId = l.PatientCaseId,
                    ClinicId = l.ClinicId,
                    FileName = l.FileName,
                    FilePath = l.FilePath,
                    ContentType = l.ContentType,
                    UploadedAt = l.UploadedAt,
                    UploadedById = l.UploadedById
                }).ToList(),
                Consents = consents,
                Appointments = new List<object>(),
                Visits = new List<object>(),
                ExportedAtUtc = DateTime.UtcNow
            };
        }

        public async Task<PatientConsentDto?> GetLatestConsentAsync(Guid patientId, Guid currentClinicId)
        {
            await EnsureClinicPatientAsync(patientId, currentClinicId);

            var consent = await _db.PatientConsents
                .AsNoTracking()
                .Where(c => c.PatientId == patientId && c.ClinicId == currentClinicId)
                .OrderByDescending(c => c.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (consent == null)
                return null;

            return new PatientConsentDto
            {
                Id = consent.Id,
                PatientId = consent.PatientId,
                ClinicId = consent.ClinicId,
                HasGivenConsent = consent.HasGivenConsent,
                ConsentType = consent.ConsentType,
                CreatedAtUtc = consent.CreatedAtUtc,
                WithdrawnAtUtc = consent.WithdrawnAtUtc,
                GivenByUserId = consent.GivenByUserId,
                Notes = consent.Notes
            };
        }

        public async Task<PatientConsentDto> AddConsentAsync(Guid patientId, Guid currentClinicId, string? currentUserId, UpsertPatientConsentRequest request)
        {
            await EnsureClinicPatientAsync(patientId, currentClinicId);

            var consent = new PatientConsent
            {
                PatientId = patientId,
                ClinicId = currentClinicId,
                HasGivenConsent = request.HasGivenConsent,
                ConsentType = string.IsNullOrWhiteSpace(request.ConsentType) ? "MedicalDataProcessing" : request.ConsentType.Trim(),
                Notes = request.Notes,
                GivenByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
                WithdrawnAtUtc = request.HasGivenConsent ? null : DateTime.UtcNow
            };

            _db.PatientConsents.Add(consent);
            await _db.SaveChangesAsync();

            return new PatientConsentDto
            {
                Id = consent.Id,
                PatientId = consent.PatientId,
                ClinicId = consent.ClinicId,
                HasGivenConsent = consent.HasGivenConsent,
                ConsentType = consent.ConsentType,
                CreatedAtUtc = consent.CreatedAtUtc,
                WithdrawnAtUtc = consent.WithdrawnAtUtc,
                GivenByUserId = consent.GivenByUserId,
                Notes = consent.Notes
            };
        }

        public async Task<PatientConsentDto> WithdrawConsentAsync(Guid patientId, Guid currentClinicId, string? currentUserId, WithdrawPatientConsentRequest request)
        {
            await EnsureClinicPatientAsync(patientId, currentClinicId);

            var latestConsent = await _db.PatientConsents
                .Where(c => c.PatientId == patientId && c.ClinicId == currentClinicId)
                .OrderByDescending(c => c.CreatedAtUtc)
                .FirstOrDefaultAsync();

            var consentType = latestConsent?.ConsentType ?? "MedicalDataProcessing";

            var withdrawn = new PatientConsent
            {
                PatientId = patientId,
                ClinicId = currentClinicId,
                HasGivenConsent = false,
                ConsentType = consentType,
                CreatedAtUtc = DateTime.UtcNow,
                WithdrawnAtUtc = DateTime.UtcNow,
                GivenByUserId = currentUserId,
                Notes = request.Notes
            };

            _db.PatientConsents.Add(withdrawn);
            await _db.SaveChangesAsync();

            return new PatientConsentDto
            {
                Id = withdrawn.Id,
                PatientId = withdrawn.PatientId,
                ClinicId = withdrawn.ClinicId,
                HasGivenConsent = withdrawn.HasGivenConsent,
                ConsentType = withdrawn.ConsentType,
                CreatedAtUtc = withdrawn.CreatedAtUtc,
                WithdrawnAtUtc = withdrawn.WithdrawnAtUtc,
                GivenByUserId = withdrawn.GivenByUserId,
                Notes = withdrawn.Notes
            };
        }

        public async Task<bool> AnonymizePatientAsync(Guid patientId, Guid currentClinicId)
        {
            var patient = await EnsureClinicPatientAsync(patientId, currentClinicId);

            var privacyState = await _db.PatientPrivacyStates
                .FirstOrDefaultAsync(ps => ps.PatientId == patientId);

            if (privacyState?.IsAnonymized == true)
                return false;

            patient.FirstName = "Anonymized";
            patient.LastName = "Patient";
            patient.Phone = null;
            patient.Gender = null;

            if (privacyState == null)
            {
                privacyState = new PatientPrivacyState
                {
                    PatientId = patientId,
                    IsDeleted = false,
                    IsAnonymized = true,
                    AnonymizedAtUtc = DateTime.UtcNow
                };
                _db.PatientPrivacyStates.Add(privacyState);
            }
            else
            {
                privacyState.IsAnonymized = true;
                privacyState.AnonymizedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<ClinicOps.Domain.Entities.Patient> EnsureClinicPatientAsync(Guid patientId, Guid clinicId)
        {
            var patient = await _db.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId);

            if (patient == null)
                throw new InvalidOperationException("Patient not found.");

            return patient;
        }
    }
}
