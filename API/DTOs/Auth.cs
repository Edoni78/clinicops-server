using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.Auth
{
    public class RegisterClinicRequest
    {
        [Required, MaxLength(200)]
        public string ClinicName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(6)]
        public string Password { get; set; } = null!;
    }
}