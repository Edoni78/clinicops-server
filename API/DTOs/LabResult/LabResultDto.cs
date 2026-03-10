namespace ClinicOps.API.DTOs.LabResult
{
    public class LabResultDto
    {
        public Guid Id { get; set; }
        public Guid PatientCaseId { get; set; }
        public string FileName { get; set; } = null!;
        /// <summary>URL to download this lab result PDF (authenticated). GET /api/PatientCase/{patientCaseId}/labresults/{id}/file</summary>
        public string DownloadUrl { get; set; } = null!;
        public string? ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? UploadedById { get; set; }
    }
}
