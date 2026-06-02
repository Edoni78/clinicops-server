using ClinicOps.Application.Services.Gdpr;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientCaseWorkflowService : IPatientCaseWorkflowService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public PatientCaseWorkflowService(ApplicationDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task<Guid> DeleteCaseAsync(Guid caseId, Guid? clinicId, ClaimsPrincipal user)
        {
            var query = _db.PatientCases.Where(pc => pc.Id == caseId);

            if (user.IsInRole("SuperAdmin"))
            {
                if (clinicId.HasValue)
                    query = query.Where(pc => pc.ClinicId == clinicId.Value);
            }
            else
            {
                var clinicIdClaim = user.FindFirst("clinicId")?.Value;
                if (string.IsNullOrWhiteSpace(clinicIdClaim) || !Guid.TryParse(clinicIdClaim, out var userClinicId))
                    throw new UnauthorizedAccessException("Forbidden");

                query = query.Where(pc => pc.ClinicId == userClinicId);
            }

            var @case = await query.FirstOrDefaultAsync();
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            _db.PatientCases.Remove(@case);
            await _db.SaveChangesAsync();

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            await _auditLogService.TryLogAsync("PatientDeleted", "PatientCase", caseId.ToString(), @case.ClinicId, userId);

            return @case.ClinicId;
        }

        public async Task<PatientCaseStatus> UpdateStatusAsync(Guid caseId, PatientCaseStatus status, Guid clinicId)
        {
            var @case = await _db.PatientCases
                .FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);

            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clinicId);
            if (clinic == null)
                throw new KeyNotFoundException("Clinic not found.");

            if (status == PatientCaseStatus.Finished)
                PatientCaseProtocolHelper.EnsureProtocolBeforeFinish(clinic, @case);

            if (status == PatientCaseStatus.Mbyllur)
            {
                if (@case.Status != PatientCaseStatus.Finished)
                    throw new InvalidOperationException(
                        "Vetëm rastet e përfunduara nga mjeku mund të mbyllen nga infermieri.");
            }
            else if (!IsAllowedTransition(@case.Status, status))
            {
                throw new InvalidOperationException(
                    $"Nuk lejohet kalimi nga {@case.Status} në {status}.");
            }

            if (status == PatientCaseStatus.InConsultation)
            {
                var anotherInConsultation = await _db.PatientCases.AnyAsync(pc =>
                    pc.ClinicId == clinicId
                    && pc.Id != caseId
                    && pc.Status == PatientCaseStatus.InConsultation);
                if (anotherInConsultation)
                    throw new InvalidOperationException("Mjeku ka tashmë një pacient në konsultim. Përfundoni vizitën aktuale para se të hapni një tjetër.");
            }

            @case.Status = status;
            if (status == PatientCaseStatus.Finished)
                @case.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return status;
        }

        private static bool IsAllowedTransition(PatientCaseStatus from, PatientCaseStatus to) =>
            (from, to) switch
            {
                (PatientCaseStatus.Waiting, PatientCaseStatus.InConsultation) => true,
                (PatientCaseStatus.InConsultation, PatientCaseStatus.Finished) => true,
                (PatientCaseStatus.Finished, PatientCaseStatus.Mbyllur) => true,
                _ => from == to,
            };
    }
}
