using ClinicOps.API.DTOs.Vitals;
using ClinicOps.Application.Services.ClinicSettings;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientCaseCommandService : IPatientCaseCommandService
    {
        private readonly ApplicationDbContext _db;

        public PatientCaseCommandService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<VitalSignsDto> SubmitVitalsAsync(Guid caseId, Guid clinicId, SubmitVitalSignsRequest request)
        {
            var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);
            if (clinic == null)
                throw new KeyNotFoundException("Clinic not found.");

            if (clinic.ClinicMode == ClinicMode.SoloDoctor)
                throw new InvalidOperationException("This clinic mode does not include nurse workflow.");

            ClinicVitalPreferencesMapper.ValidateSubmit(request, clinic);

            var @case = await _db.PatientCases
                .Include(pc => pc.Patient)
                .FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var vitals = new VitalSigns
            {
                ClinicId = clinicId,
                PatientCaseId = caseId,
                WeightKg = clinic.EnableVitalWeight ? request.WeightKg : null,
                SystolicPressure = clinic.EnableVitalBloodPressure ? request.SystolicPressure : null,
                DiastolicPressure = clinic.EnableVitalBloodPressure ? request.DiastolicPressure : null,
                TemperatureC = clinic.EnableVitalTemperature ? request.TemperatureC : null,
                HeartRate = clinic.EnableVitalHeartRate ? request.HeartRate : null,
                RecordedAt = DateTime.UtcNow
            };

            _db.VitalSigns.Add(vitals);
            await _db.SaveChangesAsync();

            return new VitalSignsDto
            {
                Id = vitals.Id,
                PatientCaseId = caseId,
                WeightKg = vitals.WeightKg,
                SystolicPressure = vitals.SystolicPressure,
                DiastolicPressure = vitals.DiastolicPressure,
                TemperatureC = vitals.TemperatureC,
                HeartRate = vitals.HeartRate,
                RecordedAt = vitals.RecordedAt
            };
        }

        public async Task<(Guid serviceId, string serviceName, decimal servicePrice)> AttachServiceAsync(Guid caseId, Guid clinicId, Guid serviceId)
        {
            var @case = await _db.PatientCases.FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var service = await _db.Services.FirstOrDefaultAsync(s =>
                s.Id == serviceId && s.ClinicId == clinicId && s.IsActive);
            if (service == null)
                throw new InvalidOperationException("Service not found, inactive, or not in this clinic.");

            @case.ServiceId = service.Id;
            await _db.SaveChangesAsync();

            return (service.Id, service.Name, service.Price);
        }
    }
}
