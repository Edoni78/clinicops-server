using ClinicOps.Domain.Enums;

namespace ClinicOps.Domain.Entities
{
    public class ClinicApplication
    {
        public int Id { get; set; }

        public string ClinicName { get; set; } = null!;
        public string AdminEmail { get; set; } = null!;
        public string AdminPasswordHash { get; set; } = null!;

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAtUtc { get; set; }

        public string? ReviewNote { get; set; }
        public ClinicMode ClinicMode { get; set; } = ClinicMode.FullTeam;
    }

    public enum ApplicationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}