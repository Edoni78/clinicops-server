using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class PatientCaseDentalFinding
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Guid PatientCaseId { get; set; }
        public PatientCase PatientCase { get; set; } = null!;

        public int ToothNumber { get; set; }

        [MaxLength(20)]
        public string Dentition { get; set; } = "Adult";

        [MaxLength(40)]
        public string Condition { get; set; } = "Healthy";

        [MaxLength(80)]
        public string? Surfaces { get; set; }

        [MaxLength(80)]
        public string? Treatment { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? UpdatedByUserId { get; set; }
    }
}
