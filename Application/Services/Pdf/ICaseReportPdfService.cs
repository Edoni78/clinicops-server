namespace ClinicOps.Application.Services.Pdf
{
    public interface ICaseReportPdfService
    {
        Task<byte[]> GenerateCaseReportPdfAsync(PatientCaseReportModel model);
    }

    public class PatientCaseReportModel
    {
        public string ClinicName { get; set; } = "";
        public string PatientFirstName { get; set; } = null!;
        public string PatientLastName { get; set; } = null!;
        public DateTime? PatientDateOfBirth { get; set; }
        public string? PatientGender { get; set; }
        public string? PatientPhone { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
        public VitalsModel? LatestVitals { get; set; }
        public MedicalReportModel? MedicalReport { get; set; }
        /// <summary>Base URL for the API (e.g. https://localhost:5258) so PDF can load signature/stamp images.</summary>
        public string? BaseUrl { get; set; }
        /// <summary>Doctor display name for "signed by" in the PDF.</summary>
        public string? DoctorDisplayName { get; set; }
        /// <summary>Relative URL of doctor signature image (e.g. /uploads/doctors/xxx/signature.png).</summary>
        public string? SignatureUrl { get; set; }
        /// <summary>Relative URL of doctor stamp image (e.g. /uploads/doctors/xxx/stamp.png).</summary>
        public string? StampUrl { get; set; }
        /// <summary>Signature image as data URI (data:image/png;base64,...) so PDF renders without loading URL.</summary>
        public string? SignatureDataUri { get; set; }
        /// <summary>Stamp image as data URI so PDF renders without loading URL.</summary>
        public string? StampDataUri { get; set; }
    }

    public class VitalsModel
    {
        public decimal? WeightKg { get; set; }
        public int? SystolicPressure { get; set; }
        public int? DiastolicPressure { get; set; }
        public decimal? TemperatureC { get; set; }
        public int? HeartRate { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class MedicalReportModel
    {
        public string? Anamneza { get; set; }
        public string Diagnosis { get; set; } = null!;
        public string Therapy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
