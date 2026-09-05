using System.Security.Claims;

namespace ClinicOps.Application.Services.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value;
    }
}
