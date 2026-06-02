using System.ComponentModel.DataAnnotations;
using ClinicOps.Domain.Enums;

namespace ClinicOps.API.DTOs.Clinic
{
    /// <summary>
    /// Clinic card/profile as returned to the dashboard.
    /// </summary>
    public class ClinicProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public ClinicMode ClinicMode { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public ClinicVitalPreferencesDto VitalPreferences { get; set; } = new();
        public ClinicProtocolPreferencesDto ProtocolPreferences { get; set; } = new();
        public ClinicColorThemePreferencesDto ColorThemePreferences { get; set; } = new();
    }

    /// <summary>
    /// Request to update clinic profile (media / card info).
    /// </summary>
    public class UpdateClinicProfileRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public ClinicVitalPreferencesDto? VitalPreferences { get; set; }
        public ClinicProtocolPreferencesDto? ProtocolPreferences { get; set; }
        public ClinicColorThemePreferencesDto? ColorThemePreferences { get; set; }
    }
}
