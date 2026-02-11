using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.ClinicUser
{
    public class ClinicUserListItemDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateClinicUserRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!; // "Doctor" | "Nurse" | "LabTechnician"
    }
}
