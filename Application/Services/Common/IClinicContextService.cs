using System.Security.Claims;

namespace ClinicOps.Application.Services.Common
{
    public interface IClinicContextService
    {
        /// <summary>
        /// Resolves clinic from token, or falls back to the default clinic (creating it if missing).
        /// Used by patient-case flows that always need a concrete clinic id.
        /// </summary>
        Task<(bool isSuperAdmin, Guid clinicId)> ResolveClinicIdAsync(ClaimsPrincipal user);

        /// <summary>
        /// Resolves clinic from token, then optional query, then existing default clinic (no create).
        /// Returns null clinicId when SuperAdmin has no query and default clinic does not exist.
        /// </summary>
        Task<(bool isSuperAdmin, Guid? clinicId)> TryResolveClinicIdAsync(
            ClaimsPrincipal user,
            Guid? fromQuery = null);

        /// <summary>
        /// Token-only clinic id (clinic users). Returns null for SuperAdmin / missing claim.
        /// </summary>
        Guid? GetClinicIdFromToken(ClaimsPrincipal user);

        /// <summary>
        /// Privacy resolution: SuperAdmin must pass clinicId query; clinic users use token.
        /// </summary>
        Guid? ResolveClinicIdForPrivacy(ClaimsPrincipal user, Guid? fromQuery = null);
    }
}
