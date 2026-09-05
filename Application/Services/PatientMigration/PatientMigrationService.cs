using System.Text.Json;
using ClinicOps.API.DTOs.PatientMigration;
using ClinicOps.Application.Services.Audit;
using ClinicOps.Application.Services.Common;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicOps.Application.Services.PatientMigrations
{
    public class PatientMigrationService : IPatientMigrationService
    {
        public const long MaxFileBytes = 20 * 1024 * 1024;
        public const int MaxRows = 50_000;
        public const int ImportBatchSize = 500;
        public static readonly TimeSpan SessionTtl = TimeSpan.FromHours(24);

        private static readonly string[] AllowedExtensions = { ".xlsx" };
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel",
            "application/octet-stream",
            "application/zip",
            "application/x-zip-compressed"
        };

        private static readonly List<PatientMigrationFieldDto> DestinationFields =
        [
            new() { Key = "firstName", Label = "Emri", Required = true },
            new() { Key = "lastName", Label = "Mbiemri", Required = true },
            new() { Key = "dateOfBirth", Label = "Data e lindjes", Required = true },
            new() { Key = "gender", Label = "Gjinia", Required = false },
            new() { Key = "phone", Label = "Telefoni", Required = false }
        ];

        private readonly ApplicationDbContext _db;
        private readonly IClinicContextService _clinicContext;
        private readonly IPatientExcelParser _excelParser;
        private readonly IPatientMigrationFileStore _fileStore;
        private readonly IAuditLogService _auditLog;

        public PatientMigrationService(
            ApplicationDbContext db,
            IClinicContextService clinicContext,
            IPatientExcelParser excelParser,
            IPatientMigrationFileStore fileStore,
            IAuditLogService auditLog)
        {
            _db = db;
            _clinicContext = clinicContext;
            _excelParser = excelParser;
            _fileStore = fileStore;
            _auditLog = auditLog;
        }

        public async Task<PatientMigrationUploadResponse> UploadAsync(
            IFormFile file,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var clinicId = RequireClinicId(user);
            await EnsureClinicActiveAsync(clinicId, cancellationToken);
            _fileStore.DeleteExpiredFiles(SessionTtl);

            ValidateUploadMetadata(file);

            await using var buffer = new MemoryStream();
            await using (var uploadStream = file.OpenReadStream())
            {
                await uploadStream.CopyToAsync(buffer, cancellationToken);
            }

            buffer.Position = 0;
            ValidateWorkbookMagic(buffer);

            buffer.Position = 0;
            var headers = _excelParser.ReadHeaders(buffer).ToList();
            if (headers.Count == 0)
                throw new InvalidOperationException("The Excel file does not contain any columns.");

            var migration = new PatientMigration
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                OriginalFileName = SanitizeFileName(file.FileName),
                StoredFileName = "workbook.xlsx",
                Status = PatientMigrationStatus.Uploaded,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = user.GetUserId()
            };

            buffer.Position = 0;
            await _fileStore.SaveExcelAsync(clinicId, migration.Id, buffer, cancellationToken);

            _db.PatientMigrations.Add(migration);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLog.TryLogAsync(
                "PatientMigrationUploaded",
                "PatientMigration",
                migration.Id.ToString(),
                clinicId,
                user.GetUserId(),
                description: $"Excel file '{migration.OriginalFileName}' uploaded for patient import.");

            return new PatientMigrationUploadResponse
            {
                MigrationId = migration.Id,
                FileName = migration.OriginalFileName,
                FileSize = file.Length,
                Headers = headers,
                Fields = DestinationFields,
                SuggestedMappings = PatientMigrationRowProcessor.SuggestMappings(headers)
            };
        }

        public async Task<PatientMigrationPreviewResponse> PreviewAsync(
            Guid migrationId,
            PatientMigrationPreviewRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var clinicId = RequireClinicId(user);
            var migration = await GetOwnedMigrationAsync(clinicId, migrationId, cancellationToken);
            EnsureNotExpired(migration);

            if (migration.Status == PatientMigrationStatus.Completed)
                throw new InvalidOperationException("This migration has already been imported.");

            if (migration.Status == PatientMigrationStatus.Processing)
                throw new InvalidOperationException("This migration is currently being imported.");

            var mappings = NormalizeMappings(request.Mappings);
            EnsureRequiredMappings(mappings);

            var excelPath = _fileStore.GetExcelPath(clinicId, migration.Id);
            if (!File.Exists(excelPath))
                throw new InvalidOperationException("The uploaded Excel file is no longer available. Please upload it again.");

            var existingKeys = await LoadExistingDuplicateIndexAsync(clinicId, cancellationToken);
            var seenNameDob = new HashSet<string>(StringComparer.Ordinal);
            var seenNameDobPhone = new HashSet<string>(StringComparer.Ordinal);
            var previewRows = new List<PatientMigrationPreviewRowDto>();
            var total = 0;
            var valid = 0;
            var invalid = 0;
            var duplicate = 0;

            await using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                foreach (var dataRow in _excelParser.ReadDataRows(stream))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    total++;
                    if (total > MaxRows)
                        throw new InvalidOperationException($"The Excel file exceeds the maximum of {MaxRows:N0} data rows.");

                    var mapped = ApplyMapping(dataRow.Values, mappings);
                    var dto = new PatientMigrationPreviewRowDto { RowNumber = dataRow.RowNumber };

                    if (!PatientMigrationRowProcessor.TryProcessRow(mapped, out var processed, out var error))
                    {
                        invalid++;
                        dto.FirstName = processed.FirstName;
                        dto.LastName = processed.LastName;
                        dto.Phone = processed.Phone;
                        dto.DateOfBirth = processed.DateOfBirth;
                        dto.Gender = processed.Gender;
                        dto.Status = "Invalid";
                        dto.Error = error;
                        previewRows.Add(dto);
                        continue;
                    }

                    dto.FirstName = processed.FirstName;
                    dto.LastName = processed.LastName;
                    dto.Phone = processed.Phone;
                    dto.DateOfBirth = processed.DateOfBirth;
                    dto.Gender = processed.Gender;

                    var nameDob = PatientMigrationRowProcessor.NameDobKey(
                        processed.FirstName!,
                        processed.LastName!,
                        processed.DateOfBirth!.Value);

                    if (IsDuplicate(nameDob, processed.Phone, seenNameDob, seenNameDobPhone)
                        || existingKeys.IsDuplicate(nameDob, processed.Phone))
                    {
                        duplicate++;
                        dto.Status = "Duplicate";
                        dto.Error = existingKeys.IsDuplicate(nameDob, processed.Phone)
                            ? "A matching patient already exists in this clinic."
                            : "Duplicate row inside the Excel file.";
                        previewRows.Add(dto);
                        continue;
                    }

                    seenNameDob.Add(nameDob);
                    if (!string.IsNullOrEmpty(processed.Phone))
                        seenNameDobPhone.Add(nameDob + "|" + processed.Phone);

                    valid++;
                    dto.Status = "Valid";
                    previewRows.Add(dto);
                }
            }

            await _fileStore.SavePreviewRowsAsync(clinicId, migration.Id, previewRows, cancellationToken);

            migration.Status = PatientMigrationStatus.Previewed;
            migration.TotalRows = total;
            migration.ValidRows = valid;
            migration.InvalidRows = invalid;
            migration.DuplicateRows = duplicate;
            migration.PreviewedAtUtc = DateTime.UtcNow;
            migration.MappingJson = JsonSerializer.Serialize(mappings);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLog.TryLogAsync(
                "PatientMigrationPreviewed",
                "PatientMigration",
                migration.Id.ToString(),
                clinicId,
                user.GetUserId(),
                description: $"Previewed patient import: {total} rows, {valid} valid, {invalid} invalid, {duplicate} duplicate.");

            const int pageSize = 25;
            return new PatientMigrationPreviewResponse
            {
                MigrationId = migration.Id,
                Status = migration.Status.ToString(),
                TotalRows = total,
                ValidRows = valid,
                InvalidRows = invalid,
                DuplicateRows = duplicate,
                Page = 1,
                PageSize = pageSize,
                RowCount = previewRows.Count,
                Rows = previewRows.Take(pageSize).ToList()
            };
        }

        public async Task<PatientMigrationRowsResponse> GetRowsAsync(
            Guid migrationId,
            string? status,
            int page,
            int pageSize,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var clinicId = RequireClinicId(user);
            var migration = await GetOwnedMigrationAsync(clinicId, migrationId, cancellationToken);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 25;
            if (pageSize > 100) pageSize = 100;

            var rows = await _fileStore.LoadPreviewRowsAsync(clinicId, migration.Id, cancellationToken);
            var filtered = FilterRows(rows, status);
            var total = filtered.Count;
            var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PatientMigrationRowsResponse
            {
                MigrationId = migration.Id,
                StatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
        }

        public async Task<PatientMigrationConfirmResponse> ConfirmAsync(
            Guid migrationId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var clinicId = RequireClinicId(user);
            await EnsureClinicActiveAsync(clinicId, cancellationToken);

            var migration = await GetOwnedMigrationAsync(clinicId, migrationId, cancellationToken, asNoTracking: true);
            EnsureNotExpired(migration);

            if (migration.Status == PatientMigrationStatus.Completed)
                return MapConfirm(migration, alreadyCompleted: true);

            if (migration.Status == PatientMigrationStatus.Processing)
                throw new InvalidOperationException("This import is already in progress.");

            if (migration.Status is not PatientMigrationStatus.Previewed and not PatientMigrationStatus.Failed)
                throw new InvalidOperationException("Please preview the import before confirming.");

            var previewRows = await _fileStore.LoadPreviewRowsAsync(clinicId, migration.Id, cancellationToken);
            var validRows = previewRows.Where(r =>
                    string.Equals(r.Status, "Valid", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(r.FirstName)
                    && !string.IsNullOrWhiteSpace(r.LastName)
                    && r.DateOfBirth.HasValue)
                .ToList();

            var claimed = await _db.PatientMigrations
                .Where(m =>
                    m.Id == migrationId
                    && m.ClinicId == clinicId
                    && (m.Status == PatientMigrationStatus.Previewed || m.Status == PatientMigrationStatus.Failed))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(m => m.Status, PatientMigrationStatus.Processing),
                    cancellationToken);

            if (claimed == 0)
            {
                var current = await GetOwnedMigrationAsync(clinicId, migrationId, cancellationToken);
                if (current.Status == PatientMigrationStatus.Completed)
                    return MapConfirm(current, alreadyCompleted: true);

                throw new InvalidOperationException("This import is already in progress.");
            }

            await _auditLog.TryLogAsync(
                "PatientMigrationStarted",
                "PatientMigration",
                migration.Id.ToString(),
                clinicId,
                user.GetUserId(),
                description: $"Started importing {validRows.Count} validated patients.");

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var existingKeys = await LoadExistingDuplicateIndexAsync(clinicId, cancellationToken);
                var toInsert = new List<Domain.Entities.Patient>(validRows.Count);
                var extraDuplicates = 0;

                foreach (var row in validRows)
                {
                    var nameDob = PatientMigrationRowProcessor.NameDobKey(
                        row.FirstName!,
                        row.LastName!,
                        row.DateOfBirth!.Value);

                    if (existingKeys.IsDuplicate(nameDob, row.Phone))
                    {
                        extraDuplicates++;
                        continue;
                    }

                    existingKeys.Add(nameDob, row.Phone);
                    toInsert.Add(new Domain.Entities.Patient
                    {
                        ClinicId = clinicId,
                        FirstName = row.FirstName!,
                        LastName = row.LastName!,
                        DateOfBirth = row.DateOfBirth.Value.Date,
                        Gender = row.Gender,
                        Phone = row.Phone,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                for (var i = 0; i < toInsert.Count; i += ImportBatchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var batch = toInsert.Skip(i).Take(ImportBatchSize).ToList();
                    _db.Patients.AddRange(batch);
                    await _db.SaveChangesAsync(cancellationToken);
                    _db.ChangeTracker.Clear();
                }

                var tracked = await _db.PatientMigrations
                    .FirstAsync(m => m.Id == migrationId && m.ClinicId == clinicId, cancellationToken);

                tracked.Status = PatientMigrationStatus.Completed;
                tracked.ImportedRows = toInsert.Count;
                tracked.DuplicateRows = migration.DuplicateRows + extraDuplicates;
                tracked.CompletedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _fileStore.DeleteExcel(clinicId, migration.Id);

                await _auditLog.TryLogAsync(
                    "PatientMigrationCompleted",
                    "PatientMigration",
                    tracked.Id.ToString(),
                    clinicId,
                    user.GetUserId(),
                    description: $"Imported {tracked.ImportedRows} patients. Duplicates skipped: {tracked.DuplicateRows}. Invalid rows: {tracked.InvalidRows}.");

                return MapConfirm(tracked, alreadyCompleted: false);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                try
                {
                    await _db.PatientMigrations
                        .Where(m => m.Id == migrationId && m.ClinicId == clinicId)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(m => m.Status, PatientMigrationStatus.Failed),
                            CancellationToken.None);
                }
                catch
                {
                    // Status update is best-effort after rollback.
                }

                await _auditLog.TryLogAsync(
                    "PatientMigrationFailed",
                    "PatientMigration",
                    migrationId.ToString(),
                    clinicId,
                    user.GetUserId(),
                    status: "Failed",
                    severity: "Warning",
                    description: "Patient import failed and was rolled back.");

                if (ex is InvalidOperationException)
                    throw;

                throw new InvalidOperationException("The import failed and no patients were added. Please try again.");
            }
        }

        public async Task<PatientMigrationStatusResponse> GetAsync(
            Guid migrationId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var clinicId = RequireClinicId(user);
            var migration = await GetOwnedMigrationAsync(clinicId, migrationId, cancellationToken);
            return MapStatus(migration);
        }

        private Guid RequireClinicId(ClaimsPrincipal user)
        {
            var clinicId = _clinicContext.GetClinicIdFromToken(user);
            if (!clinicId.HasValue)
                throw new InvalidOperationException("Only clinic users can import patients for their own clinic.");
            return clinicId.Value;
        }

        private async Task EnsureClinicActiveAsync(Guid clinicId, CancellationToken cancellationToken)
        {
            var exists = await _db.Clinics.AnyAsync(c => c.Id == clinicId && c.IsActive, cancellationToken);
            if (!exists)
                throw new InvalidOperationException("Clinic not found or inactive.");
        }

        private async Task<PatientMigration> GetOwnedMigrationAsync(
            Guid clinicId,
            Guid migrationId,
            CancellationToken cancellationToken,
            bool asNoTracking = false)
        {
            var query = _db.PatientMigrations.AsQueryable();
            if (asNoTracking)
                query = query.AsNoTracking();

            var migration = await query
                .FirstOrDefaultAsync(m => m.Id == migrationId && m.ClinicId == clinicId, cancellationToken);

            if (migration == null)
                throw new KeyNotFoundException("Migration not found.");

            return migration;
        }

        private static void EnsureNotExpired(PatientMigration migration)
        {
            if (migration.Status == PatientMigrationStatus.Completed)
                return;

            if (DateTime.UtcNow - migration.CreatedAtUtc > SessionTtl)
                throw new InvalidOperationException("This import session has expired. Please upload the Excel file again.");
        }

        private static void ValidateUploadMetadata(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Please choose a non-empty Excel file.");

            if (file.Length > MaxFileBytes)
                throw new InvalidOperationException("The Excel file exceeds the maximum size of 20 MB.");

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                throw new InvalidOperationException("Only .xlsx Excel files are supported.");

            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                var contentType = file.ContentType.Split(';')[0].Trim();
                var looksLikeExcel = AllowedContentTypes.Contains(contentType);
                var looksDangerous = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                    || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                    || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                    || contentType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase);

                if (looksDangerous && !looksLikeExcel)
                    throw new InvalidOperationException("The uploaded file type is not a supported Excel workbook.");
            }
        }

        private static void ValidateWorkbookMagic(Stream stream)
        {
            Span<byte> magic = stackalloc byte[4];
            var read = stream.Read(magic);
            if (read < 2 || magic[0] != (byte)'P' || magic[1] != (byte)'K')
                throw new InvalidOperationException("The uploaded file is not a valid Excel workbook.");
        }

        private static string SanitizeFileName(string? fileName)
        {
            var name = Path.GetFileName(fileName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return "patients.xlsx";

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Length <= 255 ? name : name[..255];
        }

        private static Dictionary<string, string> NormalizeMappings(Dictionary<string, string?>? mappings)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (mappings == null)
                return result;

            foreach (var pair in mappings)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                var field = pair.Key.Trim();
                if (!PatientMigrationRowProcessor.DestinationFieldKeys.Contains(field, StringComparer.OrdinalIgnoreCase))
                    continue;

                result[field] = pair.Value.Trim();
            }

            return result;
        }

        private static void EnsureRequiredMappings(Dictionary<string, string> mappings)
        {
            var missing = PatientMigrationRowProcessor.RequiredFieldKeys
                .Where(key => !mappings.ContainsKey(key))
                .ToList();

            if (missing.Count == 0)
                return;

            var labels = missing.Select(k => DestinationFields.First(f => f.Key.Equals(k, StringComparison.OrdinalIgnoreCase)).Label);
            throw new InvalidOperationException("Please map the required fields: " + string.Join(", ", labels) + ".");
        }

        private static Dictionary<string, object?> ApplyMapping(
            IReadOnlyDictionary<string, object?> row,
            Dictionary<string, string> mappings)
        {
            var mapped = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in PatientMigrationRowProcessor.DestinationFieldKeys)
            {
                if (!mappings.TryGetValue(field, out var header))
                    continue;

                row.TryGetValue(header, out var value);
                mapped[field] = value;
            }

            return mapped;
        }

        private async Task<PatientDuplicateIndex> LoadExistingDuplicateIndexAsync(
            Guid clinicId,
            CancellationToken cancellationToken)
        {
            var existing = await _db.Patients
                .AsNoTracking()
                .Where(p => p.ClinicId == clinicId && p.IsActive)
                .Select(p => new { p.FirstName, p.LastName, p.DateOfBirth, p.Phone })
                .ToListAsync(cancellationToken);

            var index = new PatientDuplicateIndex();
            foreach (var p in existing)
                index.Add(PatientMigrationRowProcessor.NameDobKey(p.FirstName, p.LastName, p.DateOfBirth), p.Phone);

            return index;
        }

        private static bool IsDuplicate(
            string nameDob,
            string? phone,
            HashSet<string> seenNameDob,
            HashSet<string> seenNameDobPhone)
        {
            if (string.IsNullOrEmpty(phone))
                return seenNameDob.Contains(nameDob);

            return seenNameDobPhone.Contains(nameDob + "|" + phone);
        }

        private static List<PatientMigrationPreviewRowDto> FilterRows(
            List<PatientMigrationPreviewRowDto> rows,
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase))
                return rows;

            return rows
                .Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static PatientMigrationStatusResponse MapStatus(PatientMigration migration) => new()
        {
            MigrationId = migration.Id,
            FileName = migration.OriginalFileName,
            Status = migration.Status.ToString(),
            TotalRows = migration.TotalRows,
            ValidRows = migration.ValidRows,
            InvalidRows = migration.InvalidRows,
            DuplicateRows = migration.DuplicateRows,
            ImportedRows = migration.ImportedRows,
            CreatedAtUtc = migration.CreatedAtUtc,
            PreviewedAtUtc = migration.PreviewedAtUtc,
            CompletedAtUtc = migration.CompletedAtUtc
        };

        private static PatientMigrationConfirmResponse MapConfirm(PatientMigration migration, bool alreadyCompleted) =>
            new()
            {
                MigrationId = migration.Id,
                FileName = migration.OriginalFileName,
                Status = migration.Status.ToString(),
                TotalRows = migration.TotalRows,
                ValidRows = migration.ValidRows,
                InvalidRows = migration.InvalidRows,
                DuplicateRows = migration.DuplicateRows,
                ImportedRows = migration.ImportedRows,
                CreatedAtUtc = migration.CreatedAtUtc,
                PreviewedAtUtc = migration.PreviewedAtUtc,
                CompletedAtUtc = migration.CompletedAtUtc,
                AlreadyCompleted = alreadyCompleted
            };
    }
}
