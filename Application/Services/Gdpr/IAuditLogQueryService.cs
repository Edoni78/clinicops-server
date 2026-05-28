using ClinicOps.API.DTOs.Gdpr;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Gdpr
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
