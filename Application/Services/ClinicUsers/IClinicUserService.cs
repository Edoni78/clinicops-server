using ClinicOps.API.DTOs.ClinicUser;
using System.Security.Claims;

namespace ClinicOps.Application.Services.ClinicUsers
{
    public interface IClinicUserService
    {
        Task<List<ClinicUserListItemDto>> ListAsync(ClaimsPrincipal user, Guid? clinicIdQuery = null, string? role = null);
        Task<ClinicUserListItemDto> CreateAsync(CreateClinicUserRequest request, ClaimsPrincipal user, Guid? clinicIdQuery = null);
        Task DeleteAsync(string userId, ClaimsPrincipal user, Guid? clinicIdQuery = null);
    }
}
