using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Audit
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            ApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task TryLogAsync(
            string action,
            string entityName,
            string? entityId = null,
            Guid? clinicId = null,
            string? userId = null,
            string? status = null,
            string? severity = null,
            string? description = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var resolvedUserId = userId
                    ?? httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext?.User.FindFirst("sub")?.Value;

                var ip = ResolveIpAddress(httpContext);
                var userAgent = httpContext?.Request?.Headers?.UserAgent.ToString();
                var now = DateTime.UtcNow;

                // De-duplicate noisy read events caused by repeated UI fetches.
                if (action.EndsWith("Viewed", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(resolvedUserId)
                    && !string.IsNullOrWhiteSpace(entityId))
                {
                    var windowStart = now.AddSeconds(-8);
                    var duplicateExists = await _db.AuditLogs.AsNoTracking().AnyAsync(x =>
                        x.UserId == resolvedUserId
                        && x.Action == action
                        && x.EntityId == entityId
                        && x.CreatedAtUtc >= windowStart);
                    if (duplicateExists)
                        return;
                }

                var log = new AuditLog
                {
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    ClinicId = clinicId,
                    UserId = resolvedUserId,
                    IpAddress = ip,
                    UserAgent = userAgent,
                    Status = string.IsNullOrWhiteSpace(status) ? "Success" : status,
                    Severity = string.IsNullOrWhiteSpace(severity) ? InferSeverity(action) : severity,
                    Description = string.IsNullOrWhiteSpace(description) ? BuildDescription(action, entityName, entityId) : description,
                    CreatedAtUtc = now
                };

                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit logging failed for action {Action} entity {EntityName}", action, entityName);
            }
        }

        private static string? ResolveIpAddress(HttpContext? context)
        {
            if (context == null)
                return null;

            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var first = forwarded.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(first))
                    return first;
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
                return realIp.Trim();

            return context.Connection.RemoteIpAddress?.ToString();
        }

        private static string InferSeverity(string action)
        {
            return action switch
            {
                "PatientAnonymized" => "Critical",
                "PatientDeleted" => "Critical",
                "PatientExported" => "Security",
                "FailedLogin" => "Warning",
                "PatientMigrationFailed" => "Warning",
                "PatientMigrationCompleted" => "Info",
                _ => "Info"
            };
        }

        private static string BuildDescription(string action, string entityName, string? entityId)
        {
            return action switch
            {
                "Login" => "User logged into the system successfully.",
                "FailedLogin" => "Failed login attempt.",
                "PatientExported" => "Patient data export was generated.",
                "PatientAnonymized" => "Patient personal identifiers were anonymized.",
                "PatientDeleted" => $"A {entityName} record was deleted.",
                "PatientCreated" => "A patient record was created.",
                "PatientMigrationUploaded" => "A patient Excel file was uploaded for import.",
                "PatientMigrationPreviewed" => "A patient import was previewed.",
                "PatientMigrationStarted" => "A patient import was started.",
                "PatientMigrationCompleted" => "A patient import was completed.",
                "PatientMigrationFailed" => "A patient import failed.",
                "MedicalRecordUpdated" => "A medical record was updated.",
                "MedicalRecordViewed" => "Sensitive medical data was viewed.",
                _ => $"{action} on {entityName}{(string.IsNullOrWhiteSpace(entityId) ? string.Empty : $" ({entityId})")}."
            };
        }
    }
}
