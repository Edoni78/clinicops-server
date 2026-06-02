using ClinicOps.API.DTOs.Clinic;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;

namespace ClinicOps.Application.Services.ClinicSettings
{
    public static class ClinicColorThemeMapper
    {
        private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            "default", "emerald", "teal", "violet", "rose",
        };

        public static ClinicColorThemePreferencesDto ToDto(Clinic clinic) => new()
        {
            ThemeId = ToThemeId(clinic.ColorTheme),
        };

        public static void ApplyToEntity(ClinicColorThemePreferencesDto? dto, Clinic clinic)
        {
            if (dto == null) return;
            var id = NormalizeThemeId(dto.ThemeId);
            clinic.ColorTheme = FromThemeId(id);
        }

        public static string ToThemeId(ClinicColorTheme theme) =>
            theme switch
            {
                ClinicColorTheme.Emerald => "emerald",
                ClinicColorTheme.Teal => "teal",
                ClinicColorTheme.Violet => "violet",
                ClinicColorTheme.Rose => "rose",
                _ => "default",
            };

        public static ClinicColorTheme FromThemeId(string? themeId)
        {
            var id = NormalizeThemeId(themeId);
            return id switch
            {
                "emerald" => ClinicColorTheme.Emerald,
                "teal" => ClinicColorTheme.Teal,
                "violet" => ClinicColorTheme.Violet,
                "rose" => ClinicColorTheme.Rose,
                _ => ClinicColorTheme.Default,
            };
        }

        public static string NormalizeThemeId(string? themeId)
        {
            var id = (themeId ?? "default").Trim().ToLowerInvariant();
            return Allowed.Contains(id) ? id : "default";
        }
    }
}
