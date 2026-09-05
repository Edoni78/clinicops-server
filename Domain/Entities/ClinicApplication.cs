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

        public bool CanBeReviewed() => Status == ApplicationStatus.Pending;

        public void Approve(string? reviewNote)
        {
            EnsurePending("approved");
            Status = ApplicationStatus.Approved;
            ReviewedAtUtc = DateTime.UtcNow;
            ReviewNote = reviewNote;
        }

        public void Reject(string? reviewNote)
        {
            EnsurePending("rejected");
            Status = ApplicationStatus.Rejected;
            ReviewedAtUtc = DateTime.UtcNow;
            ReviewNote = reviewNote;
        }

        private void EnsurePending(string action)
        {
            if (CanBeReviewed()) return;
            throw new InvalidOperationException(
                $"Application is already {Status}. Only pending applications can be {action}.");
        }
    }

    public enum ApplicationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
