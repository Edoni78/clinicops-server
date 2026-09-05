namespace ClinicOps.API.DTOs.PatientMigration
{
    public class PatientMigrationFieldDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Required { get; set; }
    }

    public class PatientMigrationUploadResponse
    {
        public Guid MigrationId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<PatientMigrationFieldDto> Fields { get; set; } = new();
        public Dictionary<string, string> SuggestedMappings { get; set; } = new();
    }

    public class PatientMigrationPreviewRequest
    {
        public Dictionary<string, string?> Mappings { get; set; } = new();
    }

    public class PatientMigrationPreviewRowDto
    {
        public int RowNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class PatientMigrationPreviewResponse
    {
        public Guid MigrationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public int DuplicateRows { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public List<PatientMigrationPreviewRowDto> Rows { get; set; } = new();
    }

    public class PatientMigrationRowsResponse
    {
        public Guid MigrationId { get; set; }
        public string? StatusFilter { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<PatientMigrationPreviewRowDto> Items { get; set; } = new();
    }

    public class PatientMigrationStatusResponse
    {
        public Guid MigrationId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public int DuplicateRows { get; set; }
        public int ImportedRows { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? PreviewedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public class PatientMigrationConfirmResponse : PatientMigrationStatusResponse
    {
        public bool AlreadyCompleted { get; set; }
    }
}
