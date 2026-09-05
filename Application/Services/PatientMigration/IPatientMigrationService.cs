using ClinicOps.API.DTOs.PatientMigration;
using System.Security.Claims;

namespace ClinicOps.Application.Services.PatientMigrations
{
    public interface IPatientMigrationService
    {
        Task<PatientMigrationUploadResponse> UploadAsync(
            IFormFile file,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);

        Task<PatientMigrationPreviewResponse> PreviewAsync(
            Guid migrationId,
            PatientMigrationPreviewRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);

        Task<PatientMigrationRowsResponse> GetRowsAsync(
            Guid migrationId,
            string? status,
            int page,
            int pageSize,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);

        Task<PatientMigrationConfirmResponse> ConfirmAsync(
            Guid migrationId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);

        Task<PatientMigrationStatusResponse> GetAsync(
            Guid migrationId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken);
    }
}
