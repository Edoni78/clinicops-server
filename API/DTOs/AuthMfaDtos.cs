using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.Auth
{
    public class VerifyMfaCodeRequest
    {
        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be a 6-digit number.")]
        public string Code { get; set; } = null!;
    }

    public class VerifyLoginMfaRequest
    {
        [Required]
        public string MfaTicket { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be a 6-digit number.")]
        public string Code { get; set; } = null!;
    }

    public class MfaSetupResponse
    {
        public string SharedKey { get; set; } = null!;
        public string ManualEntryKey { get; set; } = null!;
        public string QrCodeUri { get; set; } = null!;
    }

    public class MfaEnabledResponse
    {
        public bool Enabled { get; set; }
        public List<string> RecoveryCodes { get; set; } = new();
    }
}
