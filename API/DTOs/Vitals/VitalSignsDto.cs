namespace ClinicOps.API.DTOs.Vitals
{
    public class VitalSignsDto
    {
        public Guid Id { get; set; }
        public Guid PatientCaseId { get; set; }
        public decimal? WeightKg { get; set; }
        public int? SystolicPressure { get; set; }
        public int? DiastolicPressure { get; set; }
        public decimal? TemperatureC { get; set; }
        public int? HeartRate { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class SubmitVitalSignsRequest
    {
        public decimal? WeightKg { get; set; }
        public int? SystolicPressure { get; set; }
        public int? DiastolicPressure { get; set; }
        public decimal? TemperatureC { get; set; }
        public int? HeartRate { get; set; }
    }
}
