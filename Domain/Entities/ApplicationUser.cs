using Microsoft.AspNetCore.Identity;

namespace ClinicOps.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public Guid ClinicId { get; set; }

        public Clinic Clinic { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}