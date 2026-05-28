using System.Security.Claims;

namespace ClinicOps.Application.Services.Common
{
    public interface IClinicContextService
    {
        Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync(ClaimsPrincipal user);
    }
}
