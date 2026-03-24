using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;

namespace ClinicOps.API.DTOs.ClinicApplication
{
    public class ClinicApplicationDto
    {
        public int Id { get; set; }
        public string ClinicName { get; set; } = null!;
        public string AdminEmail { get; set; } = null!;
        public ApplicationStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string? ReviewNote { get; set; }
        public ClinicMode ClinicMode { get; set; }
    }

    public class ApproveRejectRequest
    {
        public string? ReviewNote { get; set; }
    }
}
