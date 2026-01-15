using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid PatientCaseId { get; set; }
        public PatientCase PatientCase { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; } // Cash, Card, etc.

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        public Guid ReceivedById { get; set; } // Nurse / Reception (ApplicationUser.Id)
    }
}