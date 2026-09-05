using System.ComponentModel.DataAnnotations;
using ClinicOps.Domain.Enums;

namespace ClinicOps.Domain.Entities
{
    public class PatientMigration
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string StoredFileName { get; set; } = string.Empty;

        public PatientMigrationStatus Status { get; set; } = PatientMigrationStatus.Uploaded;

        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public int DuplicateRows { get; set; }
        public int ImportedRows { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? PreviewedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }

        [MaxLength(2000)]
        public string? MappingJson { get; set; }
    }
}
