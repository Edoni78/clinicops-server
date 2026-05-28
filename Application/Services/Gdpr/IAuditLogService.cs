namespace ClinicOps.Application.Services.Gdpr
{
    public interface IAuditLogService
    {
        Task TryLogAsync(
            string action,
            string entityName,
            string? entityId = null,
            Guid? clinicId = null,
            string? userId = null,
            string? status = null,
            string? severity = null,
            string? description = null);
    }
}
