namespace ClinicOps.API.DTOs.MedicalReport
{
    public class MedicalReportDto
    {
        public Guid Id { get; set; }
        public Guid PatientCaseId { get; set; }
        public string? Anamneza { get; set; }
        public string Diagnosis { get; set; } = null!;
        public string Therapy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string DoctorId { get; set; } = null!;
    }

    public class SubmitMedicalReportRequest
    {
        public string? Anamneza { get; set; }
        public string Diagnosis { get; set; } = null!;
        public string Therapy { get; set; } = null!;
    }
}
