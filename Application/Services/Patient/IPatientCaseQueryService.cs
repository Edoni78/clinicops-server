using ClinicOps.API.DTOs.PatientCase;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientCaseQueryService
    {
        Task<List<PatientCaseListItemDto>> ListAsync(string? status, ClaimsPrincipal user);
        Task<PatientCaseDetailDto> GetByIdAsync(Guid caseId, ClaimsPrincipal user);
    }
}
