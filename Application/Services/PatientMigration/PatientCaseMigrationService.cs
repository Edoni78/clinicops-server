using System.Text.Json;
using ClinicOps.API.DTOs.CaseMigration;
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
    public class PatientCaseMigrationService : IPatientCaseMigrationService
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
            new() { Key = "firstName", Label = "Emri i pacientit", Required = true },
            new() { Key = "lastName", Label = "Mbiemri i pacientit", Required = true },
            new() { Key = "dateOfBirth", Label = "Data e lindjes (opsionale)", Required = false },
            new() { Key = "phone", Label = "Telefoni", Required = false },
            new() { Key = "protocolNumber", Label = "Numri i protokollit", Required = false },
            new() { Key = "notes", Label = "Shënime", Required = false },
            new() { Key = "assignedDoctor", Label = "Mjeku", Required = false },
            new() { Key = "serviceName", Label = "Shërbimi", Required = false },
            new() { Key = "createdAt", Label = "Data e rastit", Required = false },
            new() { Key = "completedAt", Label = "Data e mbylljes", Required = false }
        ];

        private readonly ApplicationDbContext _db;
        private readonly IClinicContextService _clinicContext;
        private readonly IPatientExcelParser _excelParser;
        private readonly IPatientMigrationFileStore _fileStore;
        private readonly IAuditLogService _auditLog;

        public PatientCaseMigrationService(
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
                await uploadStream.CopyToAsync(buffer, cancellationToken);

            buffer.Position = 0;
            ValidateWorkbookMagic(buffer);

            buffer.Position = 0;
            var headers = _excelParser.ReadHeaders(buffer).ToList();
            if (headers.Count == 0)
                throw new InvalidOperationException("The Excel file does not contain any columns.");

            var migration = new PatientCaseMigration
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

            _db.PatientCaseMigrations.Add(migration);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLog.TryLogAsync(
                "PatientCaseMigrationUploaded",
                "PatientCaseMigration",
                migration.Id.ToString(),
                clinicId,
                user.GetUserId(),
                description: $"Excel file '{migration.OriginalFileName}' uploaded for case import.");

            return new PatientMigrationUploadResponse
            {
                MigrationId = migration.Id,
                FileName = migration.OriginalFileName,
                FileSize = file.Length,
                Headers = headers,
                Fields = DestinationFields,
                SuggestedMappings = CaseMigrationRowProcessor.SuggestMappings(headers)
            };
        }

        public async Task<CaseMigrationPreviewResponse> PreviewAsync(
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

            var patients = await LoadPatientLookupAsync(clinicId, cancellationToken);
            var doctors = await LoadDoctorLookupAsync(clinicId, cancellationToken);
            var services = await LoadServiceLookupAsync(clinicId, cancellationToken);
            var existingProtocols = await LoadExistingProtocolsAsync(clinicId, cancellationToken);

            var seenProtocols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var previewRows = new List<CaseMigrationPreviewRowDto>();
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
                    var dto = ProcessPreviewRow(
                        dataRow.RowNumber,
                        mapped,
                        patients,
                        doctors,
                        services,
                        existingProtocols,
                        seenProtocols);

                    if (dto.Status == "Invalid") invalid++;
                    else if (dto.Status == "Duplicate") duplicate++;
                    else valid++;

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
                "PatientCaseMigrationPreviewed",
                "PatientCaseMigration",
                migration.Id.ToString(),
                clinicId,
                user.GetUserId(),
                description: $"Previewed case import: {total} rows, {valid} valid, {invalid} invalid, {duplicate} duplicate.");

            const int pageSize = 25;
            return new CaseMigrationPreviewResponse
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

        public async Task<CaseMigrationRowsResponse> GetRowsAsync(
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

            var rows = await _fileStore.LoadPreviewRowsAsync<CaseMigrationPreviewRowDto>(
                clinicId, migration.Id, cancellationToken);
            var filtered = FilterRows(rows, status);

            return new CaseMigrationRowsResponse
            {
                MigrationId = migration.Id,
                StatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status,
                Page = page,
                PageSize = pageSize,
                Total = filtered.Count,
                Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        public async Task<CaseMigrationConfirmResponse> ConfirmAsync(
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

            var previewRows = await _fileStore.LoadPreviewRowsAsync<CaseMigrationPreviewRowDto>(
                clinicId, migration.Id, cancellationToken);
            var validRows = previewRows
                .Where(r => string.Equals(r.Status, "Valid", StringComparison.OrdinalIgnoreCase) && r.PatientId.HasValue)
                .ToList();

            var claimed = await _db.PatientCaseMigrations
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
                "PatientCaseMigrationStarted",
                "PatientCaseMigration",
                migration.Id.ToString(),
                clinicId,
                user.GetUserId(),
                description: $"Started importing {validRows.Count} validated cases.");

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var activePatientIds = (await _db.Patients.AsNoTracking()
                        .Where(p => p.ClinicId == clinicId && p.IsActive)
                        .Select(p => p.Id)
                        .ToListAsync(cancellationToken))
                    .ToHashSet();

                var existingProtocols = await LoadExistingProtocolsAsync(clinicId, cancellationToken);

                var toInsert = new List<PatientCase>(validRows.Count);
                var extraDuplicates = 0;

                foreach (var row in validRows)
                {
                    if (!activePatientIds.Contains(row.PatientId!.Value))
                        continue;

                    var protocol = string.IsNullOrWhiteSpace(row.ProtocolNumber) ? null : row.ProtocolNumber.Trim();
                    if (protocol != null && !existingProtocols.Add(protocol))
                    {
                        extraDuplicates++;
                        continue;
                    }

                    var createdAt = row.CreatedAt?.Date ?? DateTime.UtcNow;
                    var completedAt = row.CompletedAt?.Date ?? createdAt;

                    toInsert.Add(new PatientCase
                    {
                        ClinicId = clinicId,
                        PatientId = row.PatientId.Value,
                        Status = CaseMigrationRowProcessor.ImportedStatus,
                        Notes = row.Notes,
                        AssignedDoctorUserId = string.IsNullOrWhiteSpace(row.AssignedDoctorUserId)
                            ? null
                            : row.AssignedDoctorUserId,
                        ServiceId = row.ServiceId,
                        ProtocolNumber = protocol,
                        CreatedAt = createdAt,
                        CompletedAt = completedAt
                    });
                }

                for (var i = 0; i < toInsert.Count; i += ImportBatchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var batch = toInsert.Skip(i).Take(ImportBatchSize).ToList();
                    _db.PatientCases.AddRange(batch);
                    await _db.SaveChangesAsync(cancellationToken);
                    _db.ChangeTracker.Clear();
                }

                var tracked = await _db.PatientCaseMigrations
                    .FirstAsync(m => m.Id == migrationId && m.ClinicId == clinicId, cancellationToken);

                tracked.Status = PatientMigrationStatus.Completed;
                tracked.ImportedRows = toInsert.Count;
                tracked.DuplicateRows = migration.DuplicateRows + extraDuplicates;
                tracked.CompletedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _fileStore.DeleteExcel(clinicId, migration.Id);

                await _auditLog.TryLogAsync(
                    "PatientCaseMigrationCompleted",
                    "PatientCaseMigration",
                    tracked.Id.ToString(),
                    clinicId,
                    user.GetUserId(),
                    description: $"Imported {tracked.ImportedRows} cases. Duplicates skipped: {tracked.DuplicateRows}. Invalid rows: {tracked.InvalidRows}.");

                return MapConfirm(tracked, alreadyCompleted: false);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                try
                {
                    await _db.PatientCaseMigrations
                        .Where(m => m.Id == migrationId && m.ClinicId == clinicId)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(m => m.Status, PatientMigrationStatus.Failed),
                            CancellationToken.None);
                }
                catch
                {
                    // Best-effort.
                }

                await _auditLog.TryLogAsync(
                    "PatientCaseMigrationFailed",
                    "PatientCaseMigration",
                    migrationId.ToString(),
                    clinicId,
                    user.GetUserId(),
                    status: "Failed",
                    severity: "Warning",
                    description: "Case import failed and was rolled back.");

                if (ex is InvalidOperationException)
                    throw;

                throw new InvalidOperationException("The import failed and no cases were added. Please try again.");
            }
        }

        public async Task<CaseMigrationStatusResponse> GetAsync(
            Guid migrationId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var clinicId = RequireClinicId(user);
            var migration = await GetOwnedMigrationAsync(clinicId, migrationId, cancellationToken);
            return MapStatus(migration);
        }

        private static CaseMigrationPreviewRowDto ProcessPreviewRow(
            int rowNumber,
            Dictionary<string, object?> mapped,
            ClinicPatientLookup patients,
            Dictionary<string, (string Id, string Name)> doctors,
            Dictionary<string, (Guid Id, string Name)> services,
            HashSet<string> existingProtocols,
            HashSet<string> seenProtocols)
        {
            var dto = new CaseMigrationPreviewRowDto { RowNumber = rowNumber };
            var errors = new List<string>();

            mapped.TryGetValue("firstName", out var firstRaw);
            mapped.TryGetValue("lastName", out var lastRaw);
            mapped.TryGetValue("dateOfBirth", out var dobRaw);
            mapped.TryGetValue("phone", out var phoneRaw);
            mapped.TryGetValue("protocolNumber", out var protocolRaw);
            mapped.TryGetValue("notes", out var notesRaw);
            mapped.TryGetValue("assignedDoctor", out var doctorRaw);
            mapped.TryGetValue("serviceName", out var serviceRaw);
            mapped.TryGetValue("createdAt", out var createdRaw);
            mapped.TryGetValue("completedAt", out var completedRaw);

            var firstName = PatientMigrationRowProcessor.NormalizeRequiredName(
                firstRaw, "First name", PatientMigrationRowProcessor.FirstNameMaxLength, out var firstError);
            if (firstError != null) errors.Add(firstError);

            var lastName = PatientMigrationRowProcessor.NormalizeRequiredName(
                lastRaw, "Last name", PatientMigrationRowProcessor.LastNameMaxLength, out var lastError);
            if (lastError != null) errors.Add(lastError);

            DateTime? dob = null;
            if (mapped.ContainsKey("dateOfBirth"))
            {
                if (!PatientMigrationRowProcessor.TryParseOptionalCalendarDate(dobRaw, "Date of birth", out dob, out var dobError))
                    errors.Add(dobError ?? "Invalid date of birth.");
            }

            var phone = PatientMigrationRowProcessor.NormalizePhone(phoneRaw, out var phoneError);
            if (phoneError != null) errors.Add(phoneError);

            dto.FirstName = firstName;
            dto.LastName = lastName;
            dto.DateOfBirth = dob;
            dto.Phone = phone;
            dto.CaseStatus = CaseMigrationRowProcessor.ImportedStatus.ToString();

            string? patientError = null;
            if (firstName != null && lastName != null)
            {
                if (patients.TryFind(firstName, lastName, dob, phone, out var patientId, out patientError))
                    dto.PatientId = patientId;
            }

            if (patientError != null)
                errors.Add(patientError);

            var protocol = CaseMigrationRowProcessor.NormalizeProtocol(protocolRaw, out var protocolError);
            if (protocolError != null) errors.Add(protocolError);
            dto.ProtocolNumber = protocol;

            var notes = CaseMigrationRowProcessor.NormalizeNotes(notesRaw, out var notesError);
            if (notesError != null) errors.Add(notesError);
            dto.Notes = notes;

            var doctorText = PatientMigrationRowProcessor.NormalizeText(doctorRaw);
            if (!string.IsNullOrEmpty(doctorText))
            {
                var doctorKey = PatientMigrationRowProcessor.NormalizeHeaderKey(doctorText);
                if (doctors.TryGetValue(doctorKey, out var doctor))
                {
                    dto.AssignedDoctorUserId = doctor.Id;
                    dto.AssignedDoctorName = doctor.Name;
                }
                else
                {
                    errors.Add($"Assigned doctor was not found in this clinic: \"{doctorText}\".");
                }
            }

            var serviceText = PatientMigrationRowProcessor.NormalizeText(serviceRaw);
            if (!string.IsNullOrEmpty(serviceText))
            {
                var serviceKey = PatientMigrationRowProcessor.NormalizeHeaderKey(serviceText);
                if (services.TryGetValue(serviceKey, out var service))
                {
                    dto.ServiceId = service.Id;
                    dto.ServiceName = service.Name;
                }
                else
                {
                    errors.Add($"Service was not found in this clinic: \"{serviceText}\".");
                }
            }

            if (!PatientMigrationRowProcessor.TryParseOptionalCalendarDate(createdRaw, "Case date", out var createdAt, out var createdError))
                errors.Add(createdError ?? "Invalid case date.");
            else
                dto.CreatedAt = createdAt;

            if (!PatientMigrationRowProcessor.TryParseOptionalCalendarDate(completedRaw, "Completed date", out var completedAt, out var completedError))
                errors.Add(completedError ?? "Invalid completed date.");
            else
                dto.CompletedAt = completedAt;

            if (dto.CreatedAt.HasValue && dto.CompletedAt.HasValue && dto.CompletedAt.Value.Date < dto.CreatedAt.Value.Date)
                errors.Add("Completed date cannot be before the case date.");

            if (errors.Count > 0)
            {
                dto.Status = "Invalid";
                dto.Error = string.Join(" ", errors);
                return dto;
            }

            if (protocol != null)
            {
                if (existingProtocols.Contains(protocol) || !seenProtocols.Add(protocol))
                {
                    dto.Status = "Duplicate";
                    dto.Error = existingProtocols.Contains(protocol)
                        ? "A case with this protocol number already exists in this clinic."
                        : "Duplicate protocol number inside the Excel file.";
                    return dto;
                }
            }

            dto.Status = "Valid";
            return dto;
        }

        private Guid RequireClinicId(ClaimsPrincipal user)
        {
            var clinicId = _clinicContext.GetClinicIdFromToken(user);
            if (!clinicId.HasValue)
                throw new InvalidOperationException("Only clinic users can import cases for their own clinic.");
            return clinicId.Value;
        }

        private async Task EnsureClinicActiveAsync(Guid clinicId, CancellationToken cancellationToken)
        {
            var exists = await _db.Clinics.AnyAsync(c => c.Id == clinicId && c.IsActive, cancellationToken);
            if (!exists)
                throw new InvalidOperationException("Clinic not found or inactive.");
        }

        private async Task<PatientCaseMigration> GetOwnedMigrationAsync(
            Guid clinicId,
            Guid migrationId,
            CancellationToken cancellationToken,
            bool asNoTracking = false)
        {
            var query = _db.PatientCaseMigrations.AsQueryable();
            if (asNoTracking)
                query = query.AsNoTracking();

            var migration = await query
                .FirstOrDefaultAsync(m => m.Id == migrationId && m.ClinicId == clinicId, cancellationToken);

            if (migration == null)
                throw new KeyNotFoundException("Migration not found.");

            return migration;
        }

        private static void EnsureNotExpired(PatientCaseMigration migration)
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
                    || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

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
                return "cases.xlsx";

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
                if (!CaseMigrationRowProcessor.DestinationFieldKeys.Contains(field, StringComparer.OrdinalIgnoreCase))
                    continue;

                result[field] = pair.Value.Trim();
            }

            return result;
        }

        private static void EnsureRequiredMappings(Dictionary<string, string> mappings)
        {
            var missing = CaseMigrationRowProcessor.RequiredFieldKeys
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
            foreach (var field in CaseMigrationRowProcessor.DestinationFieldKeys)
            {
                if (!mappings.TryGetValue(field, out var header))
                    continue;
                row.TryGetValue(header, out var value);
                mapped[field] = value;
            }

            return mapped;
        }

        private async Task<ClinicPatientLookup> LoadPatientLookupAsync(Guid clinicId, CancellationToken cancellationToken)
        {
            var rows = await _db.Patients.AsNoTracking()
                .Where(p => p.ClinicId == clinicId && p.IsActive)
                .Select(p => new { p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.Phone })
                .ToListAsync(cancellationToken);

            var lookup = new ClinicPatientLookup();
            foreach (var p in rows)
                lookup.Add(p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.Phone);
            return lookup;
        }

        private async Task<Dictionary<string, (string Id, string Name)>> LoadDoctorLookupAsync(
            Guid clinicId,
            CancellationToken cancellationToken)
        {
            var doctors = await (
                    from u in _db.Users.AsNoTracking()
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where u.ClinicId == clinicId && u.IsActive && r.Name == "Doctor"
                    select u)
                .ToListAsync(cancellationToken);

            var lookup = new Dictionary<string, (string Id, string Name)>(StringComparer.OrdinalIgnoreCase);
            foreach (var doctor in doctors)
            {
                var name = doctor.DoctorDisplayName ?? doctor.Email ?? doctor.UserName ?? doctor.Id;
                void Add(string? value)
                {
                    var key = PatientMigrationRowProcessor.NormalizeHeaderKey(value ?? string.Empty);
                    if (!string.IsNullOrEmpty(key) && !lookup.ContainsKey(key))
                        lookup[key] = (doctor.Id, name);
                }

                Add(doctor.Id);
                Add(doctor.Email);
                Add(doctor.UserName);
                Add(doctor.DoctorDisplayName);
                Add(name);
            }

            return lookup;
        }

        private async Task<Dictionary<string, (Guid Id, string Name)>> LoadServiceLookupAsync(
            Guid clinicId,
            CancellationToken cancellationToken)
        {
            var rows = await _db.Services.AsNoTracking()
                .Where(s => s.ClinicId == clinicId && s.IsActive)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync(cancellationToken);

            var lookup = new Dictionary<string, (Guid Id, string Name)>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in rows)
            {
                var key = PatientMigrationRowProcessor.NormalizeHeaderKey(s.Name);
                if (!string.IsNullOrEmpty(key) && !lookup.ContainsKey(key))
                    lookup[key] = (s.Id, s.Name);
            }

            return lookup;
        }

        private async Task<HashSet<string>> LoadExistingProtocolsAsync(Guid clinicId, CancellationToken cancellationToken)
        {
            var protocols = await _db.PatientCases.AsNoTracking()
                .Where(pc => pc.ClinicId == clinicId && pc.ProtocolNumber != null && pc.ProtocolNumber != "")
                .Select(pc => pc.ProtocolNumber!)
                .ToListAsync(cancellationToken);

            return new HashSet<string>(protocols, StringComparer.OrdinalIgnoreCase);
        }

        private static List<CaseMigrationPreviewRowDto> FilterRows(
            List<CaseMigrationPreviewRowDto> rows,
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase))
                return rows;

            return rows
                .Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static CaseMigrationStatusResponse MapStatus(PatientCaseMigration migration) => new()
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

        private static CaseMigrationConfirmResponse MapConfirm(PatientCaseMigration migration, bool alreadyCompleted) =>
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
