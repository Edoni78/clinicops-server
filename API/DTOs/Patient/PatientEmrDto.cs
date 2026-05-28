namespace ClinicOps.API.DTOs.Patient
{
    public class PatientEmrDto
    {
        public Guid PatientId { get; set; }
        public Guid ClinicId { get; set; }
        public bool IsDoctorView { get; set; }
        public bool IsReadOnly { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public List<PatientEmrConsultDto> History { get; set; } = new();
    }

    public class PatientEmrConsultDto
    {
        public Guid PatientCaseId { get; set; }
        public DateTime ConsultDate { get; set; }
        public string CaseStatus { get; set; } = null!;
        public bool CanEdit { get; set; }
        public string? Notes { get; set; }
        public string? DoctorUserId { get; set; }
        public string? DoctorName { get; set; }
        public string? Anamneza { get; set; }
        public string? Diagnosis { get; set; }
        public string? Therapy { get; set; }
        public DateTime? ReportCreatedAt { get; set; }
        public List<PatientEmrVitalsDto> Vitals { get; set; } = new();
    }

    public class PatientEmrVitalsDto
    {
        public Guid Id { get; set; }
        public decimal? WeightKg { get; set; }
        public int? SystolicPressure { get; set; }
        public int? DiastolicPressure { get; set; }
        public decimal? TemperatureC { get; set; }
        public int? HeartRate { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
