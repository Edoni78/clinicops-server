using ClinicOps.API.DTOs.Vitals;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientCaseCommandService
    {
        Task<VitalSignsDto> SubmitVitalsAsync(Guid caseId, Guid clinicId, SubmitVitalSignsRequest request);
        Task<(Guid serviceId, string serviceName, decimal servicePrice)> AttachServiceAsync(Guid caseId, Guid clinicId, Guid serviceId);
    }
}
