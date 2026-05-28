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
    }
}
