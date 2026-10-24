using ClinicOps.API.DTOs.PatientCase;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientCaseDentalService
    {
        Task<List<DentalFindingDto>> ListAsync(Guid caseId, Guid clinicId);
        Task<DentalFindingDto> UpsertAsync(Guid caseId, Guid clinicId, string? userId, UpsertDentalFindingRequest request);
        Task DeleteAsync(Guid caseId, Guid clinicId, int toothNumber, string dentition);
    }
}
