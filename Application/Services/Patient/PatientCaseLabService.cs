using ClinicOps.API.DTOs.LabResult;
using ClinicOps.Application.Services.Audit;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientCaseLabService : IPatientCaseLabService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public PatientCaseLabService(ApplicationDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task<List<LabResultDto>> ListLabResultsAsync(Guid caseId, Guid clinicId)
        {
            await EnsureLabWorkflowCaseAsync(caseId, clinicId);

            return await _db.LabResults
                .Where(l => l.PatientCaseId == caseId)
                .OrderBy(l => l.UploadedAt)
                .Select(l => new LabResultDto
                {
                    Id = l.Id,
                    PatientCaseId = l.PatientCaseId,
                    FileName = l.FileName,
                    DownloadUrl = $"/api/PatientCase/{caseId}/labresults/{l.Id}/file",
                    ContentType = l.ContentType,
                    UploadedAt = l.UploadedAt,
                    UploadedById = l.UploadedById
                })
                .ToListAsync();
        }

        public async Task<LabResultDto> UploadLabResultAsync(Guid caseId, Guid clinicId, string? userId, IFormFile file, string contentRootPath)
        {
            await EnsureLabWorkflowCaseAsync(caseId, clinicId);

            var labId = Guid.NewGuid();
            var relativePath = Path.Combine("LabUploads", caseId.ToString("N"), labId.ToString("N") + ".pdf").Replace('\\', '/');

            var labResult = new LabResult
            {
                Id = labId,
                ClinicId = clinicId,
                PatientCaseId = caseId,
                FileName = Path.GetFileName(file.FileName) ?? $"lab_{labId:N}.pdf",
                FilePath = relativePath,
                ContentType = "application/pdf",
                UploadedAt = DateTime.UtcNow,
                UploadedById = userId
            };
            _db.LabResults.Add(labResult);

            var fullPath = Path.Combine(contentRootPath ?? "", relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            await _db.SaveChangesAsync();

            return new LabResultDto
            {
                Id = labResult.Id,
                PatientCaseId = labResult.PatientCaseId,
                FileName = labResult.FileName,
                DownloadUrl = $"/api/PatientCase/{caseId}/labresults/{labResult.Id}/file",
                ContentType = labResult.ContentType,
                UploadedAt = labResult.UploadedAt,
                UploadedById = labResult.UploadedById
            };
        }

        public async Task<(byte[] bytes, string contentType, string fileName)> DownloadLabResultFileAsync(
            Guid caseId,
            Guid labId,
            Guid clinicId,
            string contentRootPath,
            string? userId)
        {
            await EnsureLabWorkflowModeAsync(clinicId);

            var lab = await _db.LabResults.FirstOrDefaultAsync(l =>
                l.Id == labId && l.PatientCaseId == caseId && l.ClinicId == clinicId);
            if (lab == null)
                throw new KeyNotFoundException("Lab result not found.");

            var fullPath = Path.Combine(contentRootPath ?? "", lab.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                throw new FileNotFoundException("Lab result file not found on disk.");

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            await _auditLogService.TryLogAsync("MedicalRecordViewed", "LabResultFile", labId.ToString(), clinicId, userId);

            return (bytes, lab.ContentType ?? "application/pdf", lab.FileName);
        }

        private async Task EnsureLabWorkflowCaseAsync(Guid caseId, Guid clinicId)
        {
            await EnsureLabWorkflowModeAsync(clinicId);
            var @case = await _db.PatientCases.FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");
        }

        private async Task EnsureLabWorkflowModeAsync(Guid clinicId)
        {
            var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clinicId);
            if (clinic == null)
                throw new KeyNotFoundException("Clinic not found.");

            clinic.EnsureLabWorkflowEnabled();
        }
    }
}
