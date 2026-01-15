using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class MedicalReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid PatientCaseId { get; set; }
        public PatientCase PatientCase { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Diagnosis { get; set; } = null!;

        [Required]
        public string Therapy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid DoctorId { get; set; } 
    }
}