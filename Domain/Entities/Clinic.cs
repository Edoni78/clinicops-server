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

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public ClinicMode ClinicMode { get; set; } = ClinicMode.FullTeam;

        public bool EnableVitalWeight { get; set; } = true;
        public bool EnableVitalBloodPressure { get; set; } = true;
        public bool EnableVitalTemperature { get; set; } = true;
        public bool EnableVitalHeartRate { get; set; } = true;

        public bool UseProtocolNumber { get; set; }
        public bool ProtocolEditableByNurse { get; set; } = true;
        public bool ProtocolEditableByDoctor { get; set; } = true;

        public ClinicColorTheme ColorTheme { get; set; } = ClinicColorTheme.Default;

        public bool IsSoloDoctor => ClinicMode == ClinicMode.SoloDoctor;

        public bool IncludesNurseWorkflow() => !IsSoloDoctor;

        public bool IncludesLabWorkflow() => !IsSoloDoctor;

        public void EnsureNurseWorkflowEnabled()
        {
            if (!IncludesNurseWorkflow())
                throw new InvalidOperationException("This clinic mode does not include nurse workflow.");
        }

        public void EnsureLabWorkflowEnabled()
        {
            if (!IncludesLabWorkflow())
                throw new InvalidOperationException("This clinic mode does not include laboratory workflow.");
        }

        public bool AllowsStaffRole(string role)
        {
            if (!IsSoloDoctor) return true;
            return role.Equals("Doctor", StringComparison.OrdinalIgnoreCase);
        }

        public void EnsureStaffRoleAllowed(string role)
        {
            if (AllowsStaffRole(role)) return;
            throw new InvalidOperationException(
                "This clinic mode does not include nurse or laboratory staff workflow.");
        }

        public bool IsProtocolRequired() => UseProtocolNumber;

        public bool CanEditProtocol(bool isClinicAdminOrSuperAdmin, bool isNurse, bool isDoctor)
        {
            if (!UseProtocolNumber) return false;
            if (isClinicAdminOrSuperAdmin) return true;
            if (isNurse && ProtocolEditableByNurse) return true;
            if (isDoctor && ProtocolEditableByDoctor) return true;
            return false;
        }

        public void EnsureCanEditProtocol(bool isClinicAdminOrSuperAdmin, bool isNurse, bool isDoctor)
        {
            if (!UseProtocolNumber)
                throw new InvalidOperationException("Numri i protokollit nuk është aktivizuar për këtë klinikë.");

            if (CanEditProtocol(isClinicAdminOrSuperAdmin, isNurse, isDoctor))
                return;

            throw new UnauthorizedAccessException("Nuk keni të drejtë të ndryshoni numrin e protokollit.");
        }

        public void EnsureVitalSubmissionAllowed(
            decimal? weightKg,
            int? systolicPressure,
            int? diastolicPressure,
            decimal? temperatureC,
            int? heartRate)
        {
            if (weightKg.HasValue && !EnableVitalWeight)
                throw new InvalidOperationException("Pesha nuk është e aktivizuar për këtë klinikë.");

            if ((systolicPressure.HasValue || diastolicPressure.HasValue) && !EnableVitalBloodPressure)
                throw new InvalidOperationException("Presioni i gjakut nuk është i aktivizuar për këtë klinikë.");

            if (temperatureC.HasValue && !EnableVitalTemperature)
                throw new InvalidOperationException("Temperatura nuk është e aktivizuar për këtë klinikë.");

            if (heartRate.HasValue && !EnableVitalHeartRate)
                throw new InvalidOperationException("Rrahjet e zemrës nuk janë të aktivizuara për këtë klinikë.");

            if (!HasAnyEnabledVital(weightKg, systolicPressure, diastolicPressure, temperatureC, heartRate))
                throw new InvalidOperationException("Jepni të paktën një shenjë vitale të aktivizuar.");
        }

        public VitalSigns CreateVitalSigns(
            Guid patientCaseId,
            decimal? weightKg,
            int? systolicPressure,
            int? diastolicPressure,
            decimal? temperatureC,
            int? heartRate)
        {
            EnsureNurseWorkflowEnabled();
            EnsureVitalSubmissionAllowed(weightKg, systolicPressure, diastolicPressure, temperatureC, heartRate);

            return new VitalSigns
            {
                ClinicId = Id,
                PatientCaseId = patientCaseId,
                WeightKg = EnableVitalWeight ? weightKg : null,
                SystolicPressure = EnableVitalBloodPressure ? systolicPressure : null,
                DiastolicPressure = EnableVitalBloodPressure ? diastolicPressure : null,
                TemperatureC = EnableVitalTemperature ? temperatureC : null,
                HeartRate = EnableVitalHeartRate ? heartRate : null,
                RecordedAt = DateTime.UtcNow
            };
        }

        private bool HasAnyEnabledVital(
            decimal? weightKg,
            int? systolicPressure,
            int? diastolicPressure,
            decimal? temperatureC,
            int? heartRate)
        {
            if (EnableVitalWeight && weightKg.HasValue) return true;
            if (EnableVitalBloodPressure && (systolicPressure.HasValue || diastolicPressure.HasValue)) return true;
            if (EnableVitalTemperature && temperatureC.HasValue) return true;
            if (EnableVitalHeartRate && heartRate.HasValue) return true;
            return false;
        }
    }
}
