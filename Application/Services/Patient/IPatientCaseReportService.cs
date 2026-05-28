using ClinicOps.API.DTOs.MedicalReport;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientCaseReportService
    {
        Task<MedicalReportDto> SubmitReportAsync(Guid caseId, Guid clinicId, string userId, SubmitMedicalReportRequest request);
        Task<MedicalReportDto> GetReportAsync(Guid caseId, Guid clinicId, string? userId);
        Task DeleteReportAsync(Guid caseId, Guid clinicId, string? userId);
    }
}
