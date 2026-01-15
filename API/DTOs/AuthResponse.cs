namespace ClinicOps.API.DTOs.Auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
    }
}