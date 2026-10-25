namespace ClinicOps.Application.Services.Pdf
{
    public interface ICaseReportPdfService
    {
        Task<byte[]> GenerateCaseReportPdfAsync(PatientCaseReportModel model);
    }

    public class PatientCaseReportModel
    {
        public string ClinicName { get; set; } = "";
        public string? ClinicAddress { get; set; }
        public string? ClinicPhone { get; set; }
        /// <summary>Relative URL of clinic logo image (e.g. /uploads/clinics/{clinicId}/logo.png).</summary>
        public string? ClinicLogoUrl { get; set; }
        /// <summary>Clinic logo image as data URI for robust PDF rendering.</summary>
        public string? ClinicLogoDataUri { get; set; }
        /// <summary>When false, the clinic logo is not drawn as a page watermark.</summary>
        public bool ShowPdfWatermark { get; set; } = true;
        public string PatientFirstName { get; set; } = null!;
        public string PatientLastName { get; set; } = null!;
        public DateTime? PatientDateOfBirth { get; set; }
        public string? PatientGender { get; set; }
        public string? PatientPhone { get; set; }
        public string Status { get; set; } = null!;
        public string? ProtocolNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
        public string? ServiceName { get; set; }
        public List<string> LabFileNames { get; set; } = new();
        public VitalsModel? LatestVitals { get; set; }
        public MedicalReportModel? MedicalReport { get; set; }
        public bool IsStomatologic { get; set; }
        public List<DentalFindingPdfModel> DentalFindings { get; set; } = new();
        /// <summary>Base URL for the API (e.g. https://localhost:5258) so PDF can load signature/stamp images.</summary>
        public string? BaseUrl { get; set; }
        /// <summary>Doctor display name for "signed by" in the PDF.</summary>
        public string? DoctorDisplayName { get; set; }
        public string? DoctorTitle { get; set; }
        public string? DoctorKoNumber { get; set; }
        /// <summary>Relative URL of doctor signature image (e.g. /uploads/doctors/xxx/signature.png).</summary>
        public string? SignatureUrl { get; set; }
        /// <summary>Relative URL of doctor stamp image (e.g. /uploads/doctors/xxx/stamp.png).</summary>
        public string? StampUrl { get; set; }
        /// <summary>Signature image as data URI (data:image/png;base64,...) so PDF renders without loading URL.</summary>
        public string? SignatureDataUri { get; set; }
        /// <summary>Stamp image as data URI so PDF renders without loading URL.</summary>
        public string? StampDataUri { get; set; }
        /// <summary>Fallback raw signature bytes if data URI parsing is not available.</summary>
        public byte[]? SignatureBytes { get; set; }
        /// <summary>Fallback raw stamp bytes if data URI parsing is not available.</summary>
        public byte[]? StampBytes { get; set; }
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

    public class DentalFindingPdfModel
    {
        public int ToothNumber { get; set; }
        public string Dentition { get; set; } = "Adult";
        public string Condition { get; set; } = "";
        public string? Surfaces { get; set; }
        public string? Treatment { get; set; }
        public string? Notes { get; set; }
    }

    public class MedicalReportModel
    {
        public string? Anamneza { get; set; }
        public string? Ekzaminimi { get; set; }
        public string Diagnosis { get; set; } = null!;
        public string Therapy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
