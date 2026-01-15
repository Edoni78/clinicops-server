using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class VitalSigns
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid PatientCaseId { get; set; }
        public PatientCase PatientCase { get; set; } = null!;

        public decimal? WeightKg { get; set; }

        public int? SystolicPressure { get; set; }
        public int? DiastolicPressure { get; set; }

        public decimal? TemperatureC { get; set; }

        public int? HeartRate { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}