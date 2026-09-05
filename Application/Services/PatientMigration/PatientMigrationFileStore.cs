using ClinicOps.API.DTOs.PatientMigration;
using System.Text.Json;

namespace ClinicOps.Application.Services.PatientMigrations
{
    public interface IPatientMigrationFileStore
    {
        string GetExcelPath(Guid clinicId, Guid migrationId);
        string GetPreviewPath(Guid clinicId, Guid migrationId);
        Task SaveExcelAsync(Guid clinicId, Guid migrationId, Stream content, CancellationToken cancellationToken);
        Task<List<PatientMigrationPreviewRowDto>> LoadPreviewRowsAsync(Guid clinicId, Guid migrationId, CancellationToken cancellationToken);
        Task SavePreviewRowsAsync(Guid clinicId, Guid migrationId, IReadOnlyList<PatientMigrationPreviewRowDto> rows, CancellationToken cancellationToken);
        void DeleteExcel(Guid clinicId, Guid migrationId);
        void DeleteSessionFiles(Guid clinicId, Guid migrationId);
        void DeleteExpiredFiles(TimeSpan maxAge);
    }

    public class PatientMigrationFileStore : IPatientMigrationFileStore
    {
        private readonly IWebHostEnvironment _env;

        public PatientMigrationFileStore(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GetExcelPath(Guid clinicId, Guid migrationId) =>
            Path.Combine(GetSessionDirectory(clinicId), $"{ToSafeId(migrationId)}.xlsx");

        public string GetPreviewPath(Guid clinicId, Guid migrationId) =>
            Path.Combine(GetSessionDirectory(clinicId), $"{ToSafeId(migrationId)}.preview.json");

        public async Task SaveExcelAsync(Guid clinicId, Guid migrationId, Stream content, CancellationToken cancellationToken)
        {
            var path = GetExcelPath(clinicId, migrationId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await content.CopyToAsync(file, cancellationToken);
        }

        public async Task<List<PatientMigrationPreviewRowDto>> LoadPreviewRowsAsync(
            Guid clinicId,
            Guid migrationId,
            CancellationToken cancellationToken)
        {
            var path = GetPreviewPath(clinicId, migrationId);
            if (!File.Exists(path))
                throw new InvalidOperationException("Preview data was not found. Please run preview again.");

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var rows = await JsonSerializer.DeserializeAsync<List<PatientMigrationPreviewRowDto>>(
                stream,
                JsonOptions(),
                cancellationToken);
            return rows ?? new List<PatientMigrationPreviewRowDto>();
        }

        public async Task SavePreviewRowsAsync(
            Guid clinicId,
            Guid migrationId,
            IReadOnlyList<PatientMigrationPreviewRowDto> rows,
            CancellationToken cancellationToken)
        {
            var path = GetPreviewPath(clinicId, migrationId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, rows, JsonOptions(), cancellationToken);
        }

        public void DeleteExcel(Guid clinicId, Guid migrationId) =>
            TryDelete(GetExcelPath(clinicId, migrationId));

        public void DeleteSessionFiles(Guid clinicId, Guid migrationId)
        {
            TryDelete(GetExcelPath(clinicId, migrationId));
            TryDelete(GetPreviewPath(clinicId, migrationId));
        }

        public void DeleteExpiredFiles(TimeSpan maxAge)
        {
            var root = GetRootDirectory();
            if (!Directory.Exists(root))
                return;

            var cutoff = DateTime.UtcNow - maxAge;
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                        File.Delete(file);
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }
        }

        private string GetSessionDirectory(Guid clinicId) =>
            Path.Combine(GetRootDirectory(), ToSafeId(clinicId));

        private string GetRootDirectory() =>
            Path.Combine(_env.ContentRootPath, "App_Data", "patient-migrations");

        private static string ToSafeId(Guid id) => id.ToString("N");

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
}
