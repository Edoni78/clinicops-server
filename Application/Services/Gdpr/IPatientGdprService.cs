using ClinicOps.API.DTOs.Gdpr;

namespace ClinicOps.Application.Services.Gdpr
{
    public interface IPatientGdprService
    {
        Task<PatientGdprExportDto> ExportPatientDataAsync(Guid patientId, Guid currentClinicId, string? currentUserId);
        Task<PatientConsentDto?> GetLatestConsentAsync(Guid patientId, Guid currentClinicId);
        Task<PatientConsentDto> AddConsentAsync(Guid patientId, Guid currentClinicId, string? currentUserId, UpsertPatientConsentRequest request);
        Task<PatientConsentDto> WithdrawConsentAsync(Guid patientId, Guid currentClinicId, string? currentUserId, WithdrawPatientConsentRequest request);
        Task<bool> AnonymizePatientAsync(Guid patientId, Guid currentClinicId);
    }
}
