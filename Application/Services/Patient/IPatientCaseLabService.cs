using ClinicOps.API.DTOs.LabResult;

namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientCaseLabService
    {
        Task<List<LabResultDto>> ListLabResultsAsync(Guid caseId, Guid clinicId);
        Task<LabResultDto> UploadLabResultAsync(Guid caseId, Guid clinicId, string? userId, IFormFile file, string contentRootPath);
        Task<(byte[] bytes, string contentType, string fileName)> DownloadLabResultFileAsync(Guid caseId, Guid labId, Guid clinicId, string contentRootPath, string? userId);
    }
}
