using ClinicOps.API.DTOs.Privacy;

namespace ClinicOps.Application.Services.Privacy
{
    public interface IPatientPrivacyService
    {
        Task<PatientPrivacyExportDto> ExportPatientDataAsync(Guid patientId, Guid currentClinicId, string? currentUserId);
        Task<PatientConsentDto?> GetLatestConsentAsync(Guid patientId, Guid currentClinicId);
        Task<PatientConsentDto> AddConsentAsync(Guid patientId, Guid currentClinicId, string? currentUserId, UpsertPatientConsentRequest request);
        Task<PatientConsentDto> WithdrawConsentAsync(Guid patientId, Guid currentClinicId, string? currentUserId, WithdrawPatientConsentRequest request);
        Task<bool> AnonymizePatientAsync(Guid patientId, Guid currentClinicId);
    }
}
