namespace ClinicOps.API.DTOs.Audit
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? ClinicId { get; set; }
        public string? UserId { get; set; }
        public string? UserDisplayName { get; set; }
        public string? UserRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityDisplayName { get; set; }
        public string? EntityReference { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Status { get; set; }
        public string? Severity { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class AuditLogListResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<AuditLogDto> Items { get; set; } = new();
    }
}
