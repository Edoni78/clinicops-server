using ClinicOps.API.DTOs.Privacy;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Privacy
{
    public class PatientPrivacyService : IPatientPrivacyService
    {
        private readonly ApplicationDbContext _db;

        public PatientPrivacyService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PatientPrivacyExportDto> ExportPatientDataAsync(
            Guid patientId,
            Guid currentClinicId,
            string? currentUserId)
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

            return new PatientPrivacyExportDto
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
                    Ekzaminimi = m.Ekzaminimi,
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

            return consent == null ? null : MapConsent(consent);
        }

        public async Task<PatientConsentDto> AddConsentAsync(
            Guid patientId,
            Guid currentClinicId,
            string? currentUserId,
            UpsertPatientConsentRequest request)
        {
            await EnsureClinicPatientAsync(patientId, currentClinicId);

            var consent = PatientConsent.Record(
                patientId,
                currentClinicId,
                request.HasGivenConsent,
                request.ConsentType,
                currentUserId,
                request.Notes);

            _db.PatientConsents.Add(consent);
            await _db.SaveChangesAsync();
            return MapConsent(consent);
        }

        public async Task<PatientConsentDto> WithdrawConsentAsync(
            Guid patientId,
            Guid currentClinicId,
            string? currentUserId,
            WithdrawPatientConsentRequest request)
        {
            await EnsureClinicPatientAsync(patientId, currentClinicId);

            var latestConsent = await _db.PatientConsents
                .Where(c => c.PatientId == patientId && c.ClinicId == currentClinicId)
                .OrderByDescending(c => c.CreatedAtUtc)
                .FirstOrDefaultAsync();

            var consentType = latestConsent?.ConsentType ?? "MedicalDataProcessing";
            var withdrawn = PatientConsent.Withdraw(
                patientId,
                currentClinicId,
                consentType,
                currentUserId,
                request.Notes);

            _db.PatientConsents.Add(withdrawn);
            await _db.SaveChangesAsync();
            return MapConsent(withdrawn);
        }

        public async Task<bool> AnonymizePatientAsync(Guid patientId, Guid currentClinicId)
        {
            var patient = await EnsureClinicPatientAsync(patientId, currentClinicId);

            var privacyState = await _db.PatientPrivacyStates
                .FirstOrDefaultAsync(ps => ps.PatientId == patientId);

            if (privacyState?.IsAnonymized == true)
                return false;

            patient.AnonymizeIdentifiers();

            if (privacyState == null)
                _db.PatientPrivacyStates.Add(PatientPrivacyState.CreateAnonymized(patientId));
            else
                privacyState.MarkAnonymized();

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<Domain.Entities.Patient> EnsureClinicPatientAsync(Guid patientId, Guid clinicId)
        {
            var patient = await _db.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.ClinicId == clinicId);

            if (patient == null)
                throw new InvalidOperationException("Patient not found.");

            return patient;
        }

        private static PatientConsentDto MapConsent(PatientConsent c) => new()
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
        };
    }
}
