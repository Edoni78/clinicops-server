using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ClinicOps.Application.Services.Common
{
    public class ProfileImageStorage : IProfileImageStorage
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly IWebHostEnvironment _env;

        public ProfileImageStorage(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveImageAsync(
            IFormFile file,
            string relativeFolder,
            string fileNameWithoutExtension,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file provided.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("Allowed formats: " + string.Join(", ", AllowedExtensions));

            var uploadsDir = Path.Combine(
                _env.WebRootPath ?? _env.ContentRootPath,
                relativeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(uploadsDir);

            var fileName = fileNameWithoutExtension + ext;
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream, cancellationToken);

            return "/" + relativeFolder.Trim('/').Replace('\\', '/') + "/" + fileName;
        }
    }
}
