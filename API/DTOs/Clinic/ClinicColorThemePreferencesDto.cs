using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.Clinic
{
    public class ClinicColorThemePreferencesDto
    {
        /// <summary>Theme id: default, emerald, teal, violet, rose.</summary>
        [MaxLength(20)]
        public string ThemeId { get; set; } = "default";
    }
}
