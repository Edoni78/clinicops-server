using ClinicOps.API.DTOs.MedicalReport;
using ClinicOps.Application.Services.Audit;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientCaseReportService : IPatientCaseReportService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public PatientCaseReportService(ApplicationDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task<MedicalReportDto> SubmitReportAsync(Guid caseId, Guid clinicId, string userId, SubmitMedicalReportRequest request)
        {
            var @case = await _db.PatientCases
                .FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);

            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var existing = await _db.MedicalReports.FirstOrDefaultAsync(m => m.PatientCaseId == caseId);
            MedicalReport report;
            if (existing != null)
            {
                existing.ApplyContent(
                    request.Anamneza,
                    request.Ekzaminimi,
                    request.Diagnosis,
                    request.Therapy,
                    userId);
                report = existing;
            }
            else
            {
                report = MedicalReport.Create(
                    clinicId,
                    caseId,
                    request.Anamneza,
                    request.Ekzaminimi,
                    request.Diagnosis,
                    request.Therapy,
                    userId);
                _db.MedicalReports.Add(report);
            }

            await _db.SaveChangesAsync();
            await _auditLogService.TryLogAsync("MedicalRecordUpdated", "MedicalReport", report.Id.ToString(), clinicId, userId);

            return new MedicalReportDto
            {
                Id = report.Id,
                PatientCaseId = caseId,
                Anamneza = report.Anamneza,
                Ekzaminimi = report.Ekzaminimi,
                Diagnosis = report.Diagnosis,
                Therapy = report.Therapy,
                CreatedAt = report.CreatedAt,
                DoctorId = report.DoctorUserId ?? userId
            };
        }

        public async Task<MedicalReportDto> GetReportAsync(Guid caseId, Guid clinicId, string? userId)
        {
            var @case = await _db.PatientCases
                .AsNoTracking()
                .FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var report = await _db.MedicalReports
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.PatientCaseId == caseId);
            if (report == null)
                throw new KeyNotFoundException("Medical report not found.");

            await _auditLogService.TryLogAsync("MedicalRecordViewed", "MedicalReport", report.Id.ToString(), clinicId, userId);

            return new MedicalReportDto
            {
                Id = report.Id,
                PatientCaseId = report.PatientCaseId,
                Anamneza = report.Anamneza,
                Ekzaminimi = report.Ekzaminimi,
                Diagnosis = report.Diagnosis,
                Therapy = report.Therapy,
                CreatedAt = report.CreatedAt,
                DoctorId = report.DoctorUserId ?? string.Empty
            };
        }

        public async Task DeleteReportAsync(Guid caseId, Guid clinicId, string? userId)
        {
            var @case = await _db.PatientCases
                .FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var report = await _db.MedicalReports.FirstOrDefaultAsync(m => m.PatientCaseId == caseId);
            if (report == null)
                throw new KeyNotFoundException("Medical report not found.");

            _db.MedicalReports.Remove(report);
            await _db.SaveChangesAsync();
            await _auditLogService.TryLogAsync("PatientDeleted", "MedicalReport", report.Id.ToString(), clinicId, userId);
        }
    }
}
