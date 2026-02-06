namespace ClinicOps.API.DTOs.Auth
{
    public class AuthClinicUserDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string? Role { get; set; }
    }
}