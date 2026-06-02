using ClinicOps.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class PatientCase
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public PatientCaseStatus Status { get; set; } = PatientCaseStatus.Waiting;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>Doctor assigned at reception when the patient is registered.</summary>
        public string? AssignedDoctorUserId { get; set; }
        public ApplicationUser? AssignedDoctor { get; set; }

        /// <summary>Optional clinic service selected by the doctor for billing (same clinic as the case).</summary>
        public Guid? ServiceId { get; set; }
        public Service? Service { get; set; }

        /// <summary>Unique protocol number per clinic when the feature is enabled (free-form text).</summary>
        [MaxLength(100)]
        public string? ProtocolNumber { get; set; }
    }
}