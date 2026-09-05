using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class PatientConsent
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Guid? ClinicId { get; set; }

        public bool HasGivenConsent { get; set; }

        [Required]
        [MaxLength(100)]
        public string ConsentType { get; set; } = "MedicalDataProcessing";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? WithdrawnAtUtc { get; set; }

        [MaxLength(450)]
        public string? GivenByUserId { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public static PatientConsent Record(
            Guid patientId,
            Guid clinicId,
            bool hasGivenConsent,
            string? consentType,
            string? givenByUserId,
            string? notes)
        {
            return new PatientConsent
            {
                PatientId = patientId,
                ClinicId = clinicId,
                HasGivenConsent = hasGivenConsent,
                ConsentType = string.IsNullOrWhiteSpace(consentType) ? "MedicalDataProcessing" : consentType.Trim(),
                Notes = notes,
                GivenByUserId = givenByUserId,
                CreatedAtUtc = DateTime.UtcNow,
                WithdrawnAtUtc = hasGivenConsent ? null : DateTime.UtcNow
            };
        }

        public static PatientConsent Withdraw(
            Guid patientId,
            Guid clinicId,
            string consentType,
            string? givenByUserId,
            string? notes)
        {
            return new PatientConsent
            {
                PatientId = patientId,
                ClinicId = clinicId,
                HasGivenConsent = false,
                ConsentType = string.IsNullOrWhiteSpace(consentType) ? "MedicalDataProcessing" : consentType,
                CreatedAtUtc = DateTime.UtcNow,
                WithdrawnAtUtc = DateTime.UtcNow,
                GivenByUserId = givenByUserId,
                Notes = notes
            };
        }
    }
}
