using ClinicOps.API.DTOs.Clinic;
using ClinicOps.Domain.Entities;

namespace ClinicOps.Application.Services.ClinicSettings
{
    public static class ClinicPreferencesMapper
    {
        public static ClinicVitalPreferencesDto ToVitalDto(Clinic clinic) =>
            ClinicVitalPreferencesMapper.ToDto(clinic);

        public static ClinicProtocolPreferencesDto ToProtocolDto(Clinic clinic) => new()
        {
            UseProtocolNumber = clinic.UseProtocolNumber,
            AllowNurseToSet = clinic.ProtocolEditableByNurse,
            AllowDoctorToSet = clinic.ProtocolEditableByDoctor,
            NextProtocolNumber = clinic.NextProtocolNumber,
        };

        public static void ApplyVital(ClinicVitalPreferencesDto? dto, Clinic clinic) =>
            ClinicVitalPreferencesMapper.ApplyToEntity(dto, clinic);

        public static void ApplyProtocol(ClinicProtocolPreferencesDto? dto, Clinic clinic)
        {
            if (dto == null) return;
            clinic.UseProtocolNumber = dto.UseProtocolNumber;
            clinic.ProtocolEditableByNurse = dto.AllowNurseToSet;
            clinic.ProtocolEditableByDoctor = dto.AllowDoctorToSet;
            if (dto.NextProtocolNumber != null)
                clinic.NextProtocolNumber = string.IsNullOrWhiteSpace(dto.NextProtocolNumber)
                    ? null
                    : dto.NextProtocolNumber.Trim();
        }

        public static ClinicColorThemePreferencesDto ToColorThemeDto(Clinic clinic) =>
            ClinicColorThemeMapper.ToDto(clinic);

        public static void ApplyColorTheme(ClinicColorThemePreferencesDto? dto, Clinic clinic) =>
            ClinicColorThemeMapper.ApplyToEntity(dto, clinic);
    }
}
