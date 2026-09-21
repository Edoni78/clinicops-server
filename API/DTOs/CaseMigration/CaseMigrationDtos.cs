namespace ClinicOps.API.DTOs.CaseMigration
{
    public class CaseMigrationPreviewRowDto
    {
        public int RowNumber { get; set; }
        public Guid? PatientId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? CaseStatus { get; set; }
        public string? ProtocolNumber { get; set; }
        public string? Notes { get; set; }
        public string? AssignedDoctorUserId { get; set; }
        public string? AssignedDoctorName { get; set; }
        public Guid? ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class CaseMigrationPreviewResponse
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
        public List<CaseMigrationPreviewRowDto> Rows { get; set; } = new();
    }

    public class CaseMigrationRowsResponse
    {
        public Guid MigrationId { get; set; }
        public string? StatusFilter { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<CaseMigrationPreviewRowDto> Items { get; set; } = new();
    }

    public class CaseMigrationStatusResponse
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

    public class CaseMigrationConfirmResponse : CaseMigrationStatusResponse
    {
        public bool AlreadyCompleted { get; set; }
    }
}
