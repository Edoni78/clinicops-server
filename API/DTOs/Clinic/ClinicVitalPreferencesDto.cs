namespace ClinicOps.API.DTOs.Clinic
{
    /// <summary>
    /// Which vital sign types the clinic uses. Nurses only see and submit enabled types.
    /// </summary>
    public class ClinicVitalPreferencesDto
    {
        public bool EnableWeight { get; set; } = true;
        public bool EnableBloodPressure { get; set; } = true;
        public bool EnableTemperature { get; set; } = true;
        public bool EnableHeartRate { get; set; } = true;
    }
}
