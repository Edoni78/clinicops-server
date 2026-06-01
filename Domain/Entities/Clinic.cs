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

        /// <summary>Clinic preference: allow nurses to record weight (kg).</summary>
        public bool EnableVitalWeight { get; set; } = true;

        /// <summary>Clinic preference: allow nurses to record blood pressure.</summary>
        public bool EnableVitalBloodPressure { get; set; } = true;

        /// <summary>Clinic preference: allow nurses to record temperature (°C).</summary>
        public bool EnableVitalTemperature { get; set; } = true;

        /// <summary>Clinic preference: allow nurses to record heart rate (bpm).</summary>
        public bool EnableVitalHeartRate { get; set; } = true;
    }
}