using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClinicOps.API.Hubs
{
    /// <summary>
    /// SignalR hub for real-time clinic updates: vitals (nurse) and reports (doctor).
    /// Clients join by clinic so doctors see nurse updates and vice versa.
    /// </summary>
    [Authorize]
    public class ClinicHub : Hub
    {
        public const string GroupPrefix = "clinic_";

        /// <summary>
        /// Join the group for a clinic. Call this after connection so you receive VitalsUpdated and ReportUpdated for that clinic.
        /// </summary>
        public async Task JoinClinic(string clinicId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupPrefix + clinicId);
        }

        /// <summary>
        /// Optional: join a specific patient case for focused view.
        /// </summary>
        public async Task JoinPatientCase(string patientCaseId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "case_" + patientCaseId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
