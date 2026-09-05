using ClinicOps.API.DTOs.Audit;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Audit
{
    public interface IAuditLogQueryService
    {
        Task<AuditLogListResponseDto> ListAsync(
            int page,
            int pageSize,
            Guid? clinicId,
            string? action,
            string? entityName,
            ClaimsPrincipal user);
    }
}
