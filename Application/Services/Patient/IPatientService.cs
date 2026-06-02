using ClinicOps.API.DTOs.Patient;
using ClinicOps.Domain.Entities;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientService
    {
        Task<PatientResponseDto> RegisterPatientAtReceptionAsync(
            Guid clinicId,
            RegisterPatientRequest request);

        Task<PatientResponseDto> OpenCaseForExistingPatientAsync(
            Guid clinicId,
            Guid patientId,
            OpenPatientCaseRequest request);
    }
}
