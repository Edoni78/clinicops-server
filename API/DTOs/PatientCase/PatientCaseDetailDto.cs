namespace ClinicOps.API.DTOs.PatientCase
{
    public class PatientCaseListItemDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientFirstName { get; set; } = null!;
        public string PatientLastName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class PatientCaseDetailDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientFirstName { get; set; } = null!;
        public string PatientLastName { get; set; } = null!;
        public DateTime? PatientDateOfBirth { get; set; }
        public string? PatientPhone { get; set; }
        public string? PatientGender { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public VitalSignsSummaryDto? LatestVitals { get; set; }
        public MedicalReportSummaryDto? MedicalReport { get; set; }
    }

    public class VitalSignsSummaryDto
    {
        public Guid Id { get; set; }
        public decimal? WeightKg { get; set; }
        public int? SystolicPressure { get; set; }
        public int? DiastolicPressure { get; set; }
        public decimal? TemperatureC { get; set; }
        public int? HeartRate { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class MedicalReportSummaryDto
    {
        public Guid Id { get; set; }
        public string? Anamneza { get; set; }
        public string Diagnosis { get; set; } = null!;
        public string Therapy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string DoctorId { get; set; } = null!;
    }
}
