using ClinicOps.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ClinicOps.Application.Services.Realtime
{
    public interface IClinicRealtimeNotifier
    {
        Task NotifyVitalsUpdatedAsync(Guid clinicId, Guid caseId, object dto);
        Task NotifyReportUpdatedAsync(Guid clinicId, Guid caseId, object dto);
        Task NotifyReportDeletedAsync(Guid clinicId, Guid caseId);
        Task NotifyCaseDeletedAsync(Guid clinicId, Guid caseId);
        Task NotifyCaseStatusChangedAsync(Guid clinicId, Guid caseId, string status);
    }

    public class ClinicRealtimeNotifier : IClinicRealtimeNotifier
    {
        private readonly IHubContext<ClinicHub> _hubContext;

        public ClinicRealtimeNotifier(IHubContext<ClinicHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyVitalsUpdatedAsync(Guid clinicId, Guid caseId, object dto) =>
            _hubContext.Clients.Group("case_" + caseId).SendAsync("VitalsUpdated", caseId, dto);

        public async Task NotifyReportUpdatedAsync(Guid clinicId, Guid caseId, object dto)
        {
            await _hubContext.Clients.Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("ReportUpdated", caseId, dto);
            await _hubContext.Clients.Group("case_" + caseId)
                .SendAsync("ReportUpdated", caseId, dto);
        }

        public async Task NotifyReportDeletedAsync(Guid clinicId, Guid caseId)
        {
            await _hubContext.Clients.Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("ReportDeleted", caseId);
            await _hubContext.Clients.Group("case_" + caseId)
                .SendAsync("ReportDeleted", caseId);
        }

        public async Task NotifyCaseDeletedAsync(Guid clinicId, Guid caseId)
        {
            await _hubContext.Clients.Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("CaseDeleted", caseId);
            await _hubContext.Clients.Group("case_" + caseId)
                .SendAsync("CaseDeleted", caseId);
        }

        public async Task NotifyCaseStatusChangedAsync(Guid clinicId, Guid caseId, string status)
        {
            await _hubContext.Clients.Group(ClinicHub.GroupPrefix + clinicId)
                .SendAsync("CaseStatusChanged", caseId, status);
            await _hubContext.Clients.Group("case_" + caseId)
                .SendAsync("CaseStatusChanged", caseId, status);
        }
    }
}
