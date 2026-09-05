namespace ClinicOps.API.DTOs.Privacy
{
    public class PatientConsentDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid? ClinicId { get; set; }
        public bool HasGivenConsent { get; set; }
        public string ConsentType { get; set; } = "MedicalDataProcessing";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? WithdrawnAtUtc { get; set; }
        public string? GivenByUserId { get; set; }
        public string? Notes { get; set; }
    }

    public class UpsertPatientConsentRequest
    {
        public bool HasGivenConsent { get; set; } = true;
        public string ConsentType { get; set; } = "MedicalDataProcessing";
        public string? Notes { get; set; }
    }

    public class WithdrawPatientConsentRequest
    {
        public string? Notes { get; set; }
    }
}
