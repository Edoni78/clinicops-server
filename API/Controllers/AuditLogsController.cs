using ClinicOps.API.DTOs.Gdpr;
using ClinicOps.Application.Services.Gdpr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogQueryService _auditLogQueryService;

        public AuditLogsController(IAuditLogQueryService auditLogQueryService)
        {
            _auditLogQueryService = auditLogQueryService;
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
            try
            {
                var result = await _auditLogQueryService.ListAsync(
                    page,
                    pageSize,
                    clinicId,
                    action,
                    entityName,
                    User);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
