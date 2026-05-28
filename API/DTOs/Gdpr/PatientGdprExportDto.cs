namespace ClinicOps.API.DTOs.Gdpr
{
    public class PatientGdprExportDto
    {
        public PatientExportProfileDto Patient { get; set; } = null!;
        public List<PatientExportCaseDto> Cases { get; set; } = new();
        public List<PatientExportMedicalReportDto> EmrRecords { get; set; } = new();
        public List<PatientExportLabResultDto> LabResults { get; set; } = new();
        public List<PatientConsentDto> Consents { get; set; } = new();
        public List<object> Appointments { get; set; } = new();
        public List<object> Visits { get; set; } = new();
        public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class PatientExportProfileDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class PatientExportCaseDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public Guid PatientId { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public Guid? ServiceId { get; set; }
    }

    public class PatientExportMedicalReportDto
    {
        public Guid Id { get; set; }
        public Guid PatientCaseId { get; set; }
        public Guid ClinicId { get; set; }
        public string? Anamneza { get; set; }
        public string Diagnosis { get; set; } = null!;
        public string Therapy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? DoctorUserId { get; set; }
    }

    public class PatientExportLabResultDto
    {
        public Guid Id { get; set; }
        public Guid PatientCaseId { get; set; }
        public Guid ClinicId { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string? ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? UploadedById { get; set; }
    }
}
