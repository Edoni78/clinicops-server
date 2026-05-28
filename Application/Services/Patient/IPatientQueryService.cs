using ClinicOps.API.DTOs.Patient;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientQueryService
    {
        Task<List<PatientResponseDto>> GetAllPatientsAsync(Guid? clinicId, ClaimsPrincipal user);
        Task<PatientEmrDto> GetPatientEmrAsync(Guid patientId, bool doctorView, ClaimsPrincipal user);
        Task DeletePatientAsync(Guid patientId, Guid? clinicId, ClaimsPrincipal user);
    }
}
