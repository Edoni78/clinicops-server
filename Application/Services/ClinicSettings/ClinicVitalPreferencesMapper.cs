using ClinicOps.API.DTOs.Clinic;
using ClinicOps.API.DTOs.PatientCase;
using ClinicOps.API.DTOs.Vitals;
using ClinicOps.Domain.Entities;

namespace ClinicOps.Application.Services.ClinicSettings
{
    public static class ClinicVitalPreferencesMapper
    {
        public static ClinicVitalPreferencesDto ToDto(Clinic clinic) => new()
        {
            EnableWeight = clinic.EnableVitalWeight,
            EnableBloodPressure = clinic.EnableVitalBloodPressure,
            EnableTemperature = clinic.EnableVitalTemperature,
            EnableHeartRate = clinic.EnableVitalHeartRate,
        };

        public static void ApplyToEntity(ClinicVitalPreferencesDto? dto, Clinic clinic)
        {
            if (dto == null) return;
            clinic.EnableVitalWeight = dto.EnableWeight;
            clinic.EnableVitalBloodPressure = dto.EnableBloodPressure;
            clinic.EnableVitalTemperature = dto.EnableTemperature;
            clinic.EnableVitalHeartRate = dto.EnableHeartRate;
        }

        public static void ValidateSubmit(SubmitVitalSignsRequest request, Clinic clinic)
        {
            clinic.EnsureVitalSubmissionAllowed(
                request.WeightKg,
                request.SystolicPressure,
                request.DiastolicPressure,
                request.TemperatureC,
                request.HeartRate);
        }

        public static bool HasAnyEnabledValue(SubmitVitalSignsRequest request, Clinic clinic)
        {
            if (clinic.EnableVitalWeight && request.WeightKg.HasValue) return true;
            if (clinic.EnableVitalBloodPressure &&
                (request.SystolicPressure.HasValue || request.DiastolicPressure.HasValue))
                return true;
            if (clinic.EnableVitalTemperature && request.TemperatureC.HasValue) return true;
            if (clinic.EnableVitalHeartRate && request.HeartRate.HasValue) return true;
            return false;
        }

        public static bool HasAnyRecordedValue(VitalSignsSummaryDto? vitals)
        {
            if (vitals == null) return false;
            if (vitals.WeightKg.HasValue) return true;
            if (vitals.SystolicPressure.HasValue || vitals.DiastolicPressure.HasValue) return true;
            if (vitals.TemperatureC.HasValue) return true;
            if (vitals.HeartRate.HasValue) return true;
            return false;
        }
    }
}
