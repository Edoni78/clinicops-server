using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.DoctorProfile
{
    /// <summary>
    /// Doctor profile for the logged-in doctor: name, signature and stamp images.
    /// </summary>
    public class DoctorProfileDto
    {
        public string UserId { get; set; } = null!;
        public string? Email { get; set; }
        /// <summary>Display name (e.g. "Dr. John Smith"). Falls back to email if not set.</summary>
        public string? DisplayName { get; set; }
        public string? Title { get; set; }
        public string? KoNumber { get; set; }
        /// <summary>URL of the doctor's signature image.</summary>
        public string? SignatureUrl { get; set; }
        /// <summary>URL of the doctor's stamp image.</summary>
        public string? StampUrl { get; set; }
    }

    public class DoctorSignoffDto
    {
        public string? Name { get; set; }
        public string? Title { get; set; }
        public string? KoNumber { get; set; }
        public string? SignatureUrl { get; set; }
        public string? StampUrl { get; set; }
        public string? SignatureBase64 { get; set; }
        public string? StampBase64 { get; set; }
    }

    /// <summary>
    /// Request to update doctor profile (display name only; signature/stamp via upload endpoints).
    /// </summary>
    public class UpdateDoctorProfileRequest
    {
        [MaxLength(200)]
        public string? DisplayName { get; set; }

        [MaxLength(80)]
        public string? Title { get; set; }

        [MaxLength(50)]
        public string? KoNumber { get; set; }
    }
}
