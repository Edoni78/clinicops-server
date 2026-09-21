using ClinicOps.API.DTOs.CaseMigration;
using ClinicOps.API.DTOs.PatientMigration;
using System.Security.Claims;

namespace ClinicOps.Application.Services.PatientMigrations
{
    public interface IPatientCaseMigrationService
    {
        Task<PatientMigrationUploadResponse> UploadAsync(
            IFormFile file,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);

        Task<CaseMigrationPreviewResponse> PreviewAsync(
            Guid migrationId,
            PatientMigrationPreviewRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);

        Task<CaseMigrationRowsResponse> GetRowsAsync(
            Guid migrationId,
            string? status,
            int page,
            int pageSize,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);

        Task<CaseMigrationConfirmResponse> ConfirmAsync(
            Guid migrationId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);

        Task<CaseMigrationStatusResponse> GetAsync(
            Guid migrationId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);
    }
}
