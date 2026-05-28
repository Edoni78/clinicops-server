namespace ClinicOps.Application.Services.Patient
{
    public interface IPatientCasePdfFacadeService
    {
        Task<(byte[] fileBytes, string fileName)> GenerateCaseReportPdfAsync(Guid caseId, Guid clinicId, string baseUrl, string? fallbackDoctorUserId);
    }
}
