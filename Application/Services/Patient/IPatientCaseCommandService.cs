using ClinicOps.API.DTOs.Vitals;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientCaseCommandService
    {
        Task<VitalSignsDto> SubmitVitalsAsync(Guid caseId, Guid clinicId, SubmitVitalSignsRequest request);
        Task<(Guid serviceId, string serviceName, decimal servicePrice)> AttachServiceAsync(Guid caseId, Guid clinicId, Guid serviceId);
        Task<string> UpdateProtocolNumberAsync(Guid caseId, Guid clinicId, string protocolNumber, ClaimsPrincipal user);
    }
}
