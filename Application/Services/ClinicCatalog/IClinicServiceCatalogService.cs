using ClinicOps.API.DTOs.Service;
using System.Security.Claims;

namespace ClinicOps.Application.Services.ClinicCatalog
{
    public interface IClinicServiceCatalogService
    {
        Task<List<ServiceDto>> ListAsync(ClaimsPrincipal user, Guid? clinicIdQuery = null);
        Task<ServiceDto> GetByIdAsync(Guid id, ClaimsPrincipal user, Guid? clinicIdQuery = null);
        Task<ServiceDto> CreateAsync(CreateServiceRequest request, ClaimsPrincipal user, Guid? clinicIdQuery = null);
        Task<ServiceDto> UpdateAsync(Guid id, UpdateServiceRequest request, ClaimsPrincipal user, Guid? clinicIdQuery = null);
        Task DeleteAsync(Guid id, ClaimsPrincipal user, Guid? clinicIdQuery = null);
    }
}
