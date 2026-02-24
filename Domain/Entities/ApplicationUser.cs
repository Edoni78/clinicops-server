using Microsoft.AspNetCore.Identity;

namespace ClinicOps.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public Guid? ClinicId { get; set; }

        public Clinic Clinic { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        /// <summary>Display name for doctor (e.g. "Dr. John Smith"). Used in reports and UI.</summary>
        public string? DoctorDisplayName { get; set; }

        /// <summary>URL of the doctor's signature image (uploaded to wwwroot/uploads/doctors/{userId}/).</summary>
        public string? SignatureUrl { get; set; }

        /// <summary>URL of the doctor's stamp image (uploaded to wwwroot/uploads/doctors/{userId}/).</summary>
        public string? StampUrl { get; set; }
    }
}