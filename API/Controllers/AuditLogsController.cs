using ClinicOps.API.DTOs.Gdpr;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AuditLogsController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// List audit logs with paging. Clinic users only see their clinic; SuperAdmin can pass clinicId.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(AuditLogListResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AuditLogListResponseDto>> List(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? clinicId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityName = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            Guid? resolvedClinicId;
            if (User.IsInRole("SuperAdmin"))
            {
                resolvedClinicId = clinicId;
            }
            else
            {
                var clinicIdClaim = User.FindFirst("clinicId")?.Value;
                if (string.IsNullOrWhiteSpace(clinicIdClaim) || !Guid.TryParse(clinicIdClaim, out var userClinicId))
                    return Forbid();

                resolvedClinicId = userClinicId;
            }

            var query = _db.AuditLogs.AsNoTracking().AsQueryable();
            if (resolvedClinicId.HasValue)
                query = query.Where(x => x.ClinicId == resolvedClinicId.Value);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(x => x.Action == action);

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(x => x.EntityName == entityName);

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = logs
                .Select(x => x.UserId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .Distinct()
                .ToList();

            var users = await _db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            var userRoles = await _db.Set<IdentityUserRole<string>>()
                .AsNoTracking()
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(
                    _db.Set<IdentityRole>().AsNoTracking(),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, Role = r.Name })
                .ToListAsync();

            var userRoleLookup = userRoles
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Role).FirstOrDefault() ?? "Unknown");

            var userLookup = users.ToDictionary(
                u => u.Id,
                u => new
                {
                    DisplayName = !string.IsNullOrWhiteSpace(u.DoctorDisplayName)
                        ? u.DoctorDisplayName
                        : (!string.IsNullOrWhiteSpace(u.Email) ? u.Email : u.UserName ?? u.Id),
                    Role = userRoleLookup.TryGetValue(u.Id, out var role) ? role : "Unknown"
                });

            var patientCaseIds = logs
                .Where(x => x.EntityName == "PatientCase")
                .Select(x => x.EntityId)
                .Where(x => Guid.TryParse(x, out _))
                .Select(x => Guid.Parse(x!))
                .Distinct()
                .ToList();

            var caseLookup = await _db.PatientCases
                .AsNoTracking()
                .Where(pc => patientCaseIds.Contains(pc.Id))
                .Join(
                    _db.Patients.AsNoTracking(),
                    pc => pc.PatientId,
                    p => p.Id,
                    (pc, p) => new { pc.Id, PatientName = p.FirstName + " " + p.LastName })
                .ToDictionaryAsync(x => x.Id, x => x.PatientName);

            var medicalReportIds = logs
                .Where(x => x.EntityName == "MedicalReport")
                .Select(x => x.EntityId)
                .Where(x => Guid.TryParse(x, out _))
                .Select(x => Guid.Parse(x!))
                .Distinct()
                .ToList();

            var reportLookup = await _db.MedicalReports
                .AsNoTracking()
                .Where(r => medicalReportIds.Contains(r.Id))
                .Join(
                    _db.PatientCases.AsNoTracking(),
                    r => r.PatientCaseId,
                    pc => pc.Id,
                    (r, pc) => new { r.Id, r.PatientCaseId, pc.PatientId })
                .Join(
                    _db.Patients.AsNoTracking(),
                    x => x.PatientId,
                    p => p.Id,
                    (x, p) => new { x.Id, x.PatientCaseId, PatientName = p.FirstName + " " + p.LastName })
                .ToDictionaryAsync(x => x.Id, x => new { x.PatientCaseId, x.PatientName });

            var items = logs.Select(x =>
            {
                var hasUserId = !string.IsNullOrWhiteSpace(x.UserId);
                var userDisplayName = hasUserId && userLookup.TryGetValue(x.UserId!, out var userInfo)
                    ? userInfo.DisplayName
                    : (hasUserId ? $"User {x.UserId}" : "System");
                var userRole = hasUserId && userLookup.TryGetValue(x.UserId!, out var userRoleInfo)
                    ? userRoleInfo.Role
                    : (x.Action == "Login" || x.Action == "FailedLogin" ? "AuthenticatedUser" : null);

                var entityDisplayName = x.EntityName;
                string? entityReference = null;
                if (x.EntityName == "PatientCase" && Guid.TryParse(x.EntityId, out var caseId) && caseLookup.TryGetValue(caseId, out var patientName))
                {
                    entityDisplayName = "Patient Case";
                    entityReference = $"Patient: {patientName}";
                }
                else if (x.EntityName == "MedicalReport" && Guid.TryParse(x.EntityId, out var reportId) && reportLookup.TryGetValue(reportId, out var reportCtx))
                {
                    entityDisplayName = "Medical Report";
                    entityReference = $"Patient: {reportCtx.PatientName}";
                }

                return new AuditLogDto
                {
                    Id = x.Id,
                    ClinicId = x.ClinicId,
                    UserId = x.UserId,
                    UserDisplayName = userDisplayName,
                    UserRole = userRole,
                    Action = x.Action,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    EntityDisplayName = entityDisplayName,
                    EntityReference = entityReference,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    Status = x.Status ?? "Success",
                    Severity = x.Severity ?? "Info",
                    Description = x.Description ?? $"{x.Action} on {entityDisplayName}.",
                    CreatedAtUtc = x.CreatedAtUtc
                };
            }).ToList();

            return Ok(new AuditLogListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            });
        }
    }
}
