using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class LabResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid PatientCaseId { get; set; }
        public PatientCase PatientCase { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = null!;

        [MaxLength(100)]
        public string? ContentType { get; set; } // application/pdf

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Guid UploadedById { get; set; } // LabTechnician (ApplicationUser.Id)
    }
}