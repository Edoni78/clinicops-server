using ClinicOps.API.DTOs.Vitals;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

            var @case = await _db.PatientCases
                .FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var vitals = clinic.CreateVitalSigns(
                caseId,
                request.WeightKg,
                request.SystolicPressure,
                request.DiastolicPressure,
                request.TemperatureC,
                request.HeartRate);

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

        public async Task<(Guid serviceId, string serviceName, decimal servicePrice)> AttachServiceAsync(
            Guid caseId,
            Guid clinicId,
            Guid serviceId)
        {
            var @case = await _db.PatientCases.FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var service = await _db.Services.FirstOrDefaultAsync(s =>
                s.Id == serviceId && s.ClinicId == clinicId && s.IsActive);
            if (service == null)
                throw new InvalidOperationException("Service not found, inactive, or not in this clinic.");

            @case.AttachService(service.Id);
            await _db.SaveChangesAsync();

            return (service.Id, service.Name, service.Price);
        }

        public async Task<string> UpdateProtocolNumberAsync(
            Guid caseId,
            Guid clinicId,
            string protocolNumber,
            ClaimsPrincipal user)
        {
            var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);
            if (clinic == null)
                throw new KeyNotFoundException("Clinic not found.");

            clinic.EnsureCanEditProtocol(
                user.IsInRole("ClinicAdmin") || user.IsInRole("SuperAdmin"),
                user.IsInRole("Nurse"),
                user.IsInRole("Doctor"));

            var normalized = Domain.Entities.PatientCase.NormalizeProtocolNumber(protocolNumber);

            var @case = await _db.PatientCases.FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var duplicate = await _db.PatientCases.AnyAsync(pc =>
                pc.ClinicId == clinicId
                && pc.Id != caseId
                && pc.ProtocolNumber != null
                && pc.ProtocolNumber.ToLower() == normalized.ToLower());

            if (duplicate)
                throw new InvalidOperationException("Ky numër protokolli ekziston tashmë për një rast tjetër në klinikë.");

            @case.SetProtocolNumber(normalized);
            await _db.SaveChangesAsync();
            return normalized;
        }
    }
}
