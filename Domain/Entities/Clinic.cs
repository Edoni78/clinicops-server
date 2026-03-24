using ClinicOps.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class Clinic
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        /// <summary>Optional logo URL (e.g. from upload or external URL).</summary>
        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        /// <summary>Short description / info for the clinic card.</summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public ClinicMode ClinicMode { get; set; } = ClinicMode.FullTeam;
    }
}