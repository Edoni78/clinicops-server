namespace ClinicOps.API.DTOs.Auth
{
    public class AuthResponse
    {
        public string? AccessToken { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public AuthClinicUserDto? User { get; set; }
        public bool RequiresMfa { get; set; }
        public string? MfaTicket { get; set; }
    }
}