using ClinicOps.API.DTOs.ClinicApplication;

namespace ClinicOps.Application.Services.ClinicApplications
{
    public interface IClinicApplicationService
    {
        Task<List<ClinicApplicationDto>> ListAsync(string? status = null);
        Task<ClinicApplicationApproveResult> ApproveAsync(int id, string? reviewNote = null);
        Task RejectAsync(int id, string? reviewNote = null);
    }

    public class ClinicApplicationApproveResult
    {
        public Guid ClinicId { get; set; }
        public string AdminUserId { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
