using Microsoft.AspNetCore.Http;

namespace ClinicOps.Application.Services.Common
{
    public interface IProfileImageStorage
    {
        /// <summary>
        /// Validates and saves an image under wwwroot/{relativeFolder}/{fileNameWithoutExt}{ext}.
        /// Returns the public URL path (e.g. /uploads/clinics/{id}/logo.png).
        /// </summary>
        Task<string> SaveImageAsync(
            IFormFile file,
            string relativeFolder,
            string fileNameWithoutExtension,
            CancellationToken cancellationToken = default);
    }
}
