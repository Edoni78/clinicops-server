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
            if (request.WeightKg.HasValue && !clinic.EnableVitalWeight)
                throw new InvalidOperationException("Pesha nuk është e aktivizuar për këtë klinikë.");

            if ((request.SystolicPressure.HasValue || request.DiastolicPressure.HasValue) && !clinic.EnableVitalBloodPressure)
                throw new InvalidOperationException("Presioni i gjakut nuk është i aktivizuar për këtë klinikë.");

            if (request.TemperatureC.HasValue && !clinic.EnableVitalTemperature)
                throw new InvalidOperationException("Temperatura nuk është e aktivizuar për këtë klinikë.");

            if (request.HeartRate.HasValue && !clinic.EnableVitalHeartRate)
                throw new InvalidOperationException("Rrahjet e zemrës nuk janë të aktivizuara për këtë klinikë.");

            if (!HasAnyEnabledValue(request, clinic))
                throw new InvalidOperationException("Jepni të paktën një shenjë vitale të aktivizuar.");
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
