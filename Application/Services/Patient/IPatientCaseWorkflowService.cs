using ClinicOps.Domain.Enums;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientCaseWorkflowService
    {
        Task<Guid> DeleteCaseAsync(Guid caseId, Guid? clinicId, ClaimsPrincipal user);
        Task<PatientCaseStatus> UpdateStatusAsync(
            Guid caseId,
            PatientCaseStatus status,
            Guid clinicId,
            ClaimsPrincipal user);
    }
}
