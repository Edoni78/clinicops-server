namespace ClinicOps.API.DTOs.Clinic
{
    /// <summary>
    /// Clinic settings for case protocol numbers (numri i protokollit).
    /// </summary>
    public class ClinicProtocolPreferencesDto
    {
        /// <summary>When true, cases use a unique protocol number per clinic.</summary>
        public bool UseProtocolNumber { get; set; }

        /// <summary>Nurse can enter or change the protocol number.</summary>
        public bool AllowNurseToSet { get; set; } = true;

        /// <summary>Doctor can enter or change the protocol number.</summary>
        public bool AllowDoctorToSet { get; set; } = true;

        /// <summary>Next protocol number to assign when opening a case.</summary>
        public string? NextProtocolNumber { get; set; }
    }
}
